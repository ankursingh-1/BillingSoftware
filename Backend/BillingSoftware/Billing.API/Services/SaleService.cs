using Billing.API.Models;
using Billing.Application.Accounting;
using Billing.Application.Documents;
using Billing.Application.DTOs.Sales;
using Billing.Application.DTOs.Warranty;
using Billing.Application.Interfaces;
using Billing.Application.Retail;
using Billing.Application.Warranty;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

/// <summary>
/// Sales: invoice creation, listing, details and payments.
///
/// Money rules live in <see cref="GstCalculator"/>; this class only gathers the facts
/// (catalogue prices, tax rates, company/customer state) and records the result.
///
/// Concurrency (no application-wide lock is needed):
///  - stock moves with one atomic conditional UPDATE (see InventoryService);
///  - the invoice number comes from a row in DocumentSequences updated inside the same
///    transaction, so two sales can never share a number and a failed sale gives its
///    number back;
///  - a unique index on InvoiceNumber and on ClientRequestId backs both up in the database;
///  - payments and voids first take an exclusive row lock on the sale.
/// </summary>
public class SaleService
{
    private const int MaxPageSize = 100;

    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly IDocumentNumberService _numbers;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public SaleService(
        BillingDbContext context,
        IInventoryService inventoryService,
        IDocumentNumberService numbers,
        IAuditService audit,
        ICurrentUser user)
    {
        _context = context;
        _inventoryService = inventoryService;
        _numbers = numbers;
        _audit = audit;
        _user = user;
    }

    // ------------------------------------------------------------------
    // Create
    // ------------------------------------------------------------------

    public async Task<SaleResponse> CreateAsync(CreateSaleRequest request)
    {
        // Fast path: this exact submission was already processed.
        if (request.ClientRequestId is { } requestId)
        {
            var existing = await FindByRequestIdAsync(requestId);
            if (existing != null)
                return existing;
        }

        try
        {
            return await CreateCoreAsync(request);
        }
        catch (DbUpdateException ex) when (
            request.ClientRequestId.HasValue && IsUniqueViolation(ex))
        {
            // Two identical submissions raced; the database let only one through.
            var existing = await FindByRequestIdAsync(request.ClientRequestId.Value);
            if (existing != null)
                return existing;

            throw;
        }
    }

    private async Task<SaleResponse> CreateCoreAsync(CreateSaleRequest request)
    {
        // Merge repeated product lines so each product is validated, priced
        // and deducted from stock exactly once.
        var lines = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => MapLine(g))
            .ToList();

        var payments = request.Payments ?? new List<CreateSalePaymentRequest>();

        foreach (var payment in payments)
        {
            if (GstCalculator.Round(payment.Amount) != payment.Amount)
                throw new BusinessException("Payment amounts can have at most 2 decimal places.");
        }

        // Disposing an uncommitted transaction rolls it back, so any rejection
        // below leaves no sale, no stock change, no payment and no used number.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        // ---- Validate everything before writing anything ----

        var customer = await _context.Customers
            .FirstOrDefaultAsync(x => x.Id == request.CustomerId && !x.IsDeleted)
            ?? throw new BusinessException("Customer not found or has been deleted.");

        var company = await _context.Companies
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync()
            ?? throw BusinessException.Conflict(
                "Company settings are not configured. Open Settings and save your company details before billing.");

        var prefix = DocumentNumbering.NormalizePrefix(company.InvoicePrefix)
            ?? throw BusinessException.Conflict(
                "The invoice prefix in Settings must be 1 to 4 letters or digits (invoice numbers are limited to 16 characters).");

        var productIds = lines.Select(l => l.ProductId).ToList();

        var products = await _context.Products
            .Where(x => productIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id);

        var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();

        if (missing.Count > 0)
        {
            throw new BusinessException(
                $"Product not found or has been deleted (id: {string.Join(", ", missing)}).");
        }

        var taxIds = products.Values
            .Where(p => p.TaxId.HasValue)
            .Select(p => p.TaxId!.Value)
            .Distinct()
            .ToList();

        var taxes = await _context.Taxes
            .AsNoTracking()
            .Where(t => taxIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        var conflicts = new List<string>();

        foreach (var line in lines)
        {
            var product = products[line.ProductId];

            // A client-supplied price is only an echo of what the user saw.
            // It never sets the price; a mismatch means the cart is stale.
            if (line.EchoedPrices.Any(p => p != product.SellingPrice))
            {
                conflicts.Add(
                    $"The price of '{product.Name}' has changed to {product.SellingPrice:0.00}. Refresh and try again.");
            }

            if (product.Stock <= 0)
            {
                conflicts.Add($"'{product.Name}' is out of stock.");
            }
            else if (product.Stock < line.Quantity)
            {
                conflicts.Add(
                    $"'{product.Name}': only {product.Stock} in stock, {line.Quantity} requested.");
            }

            if (product.TaxId is { } taxId &&
                (!taxes.TryGetValue(taxId, out var tax) || tax.IsDeleted || !tax.IsActive))
            {
                conflicts.Add(
                    $"The tax assigned to '{product.Name}' is inactive or removed. Fix the product's tax before selling it.");
            }
        }

        if (conflicts.Count > 0)
        {
            throw BusinessException.Conflict(string.Join(" ", conflicts));
        }

        // ---- Place of supply: decides CGST+SGST (same state) or IGST (other state) ----

        var companyState = PlaceOfSupply.CompanyStateCode(
            company.StateCode, company.State, company.GSTNumber);

        var customerState = PlaceOfSupply.PartyStateCode(
            customer.StateCode, customer.State, customer.Gstin);

        var placeOfSupply = PlaceOfSupply.Resolve(customerState, companyState);
        var isInterState = PlaceOfSupply.IsInterState(placeOfSupply, companyState);

        var hasTaxableLine = products.Values.Any(p =>
            p.TaxId.HasValue && taxes[p.TaxId.Value].Percentage > 0);

        if (hasTaxableLine && companyState == null)
        {
            throw BusinessException.Conflict(
                "Set your company's state in Settings before selling taxable items (needed to choose CGST/SGST or IGST).");
        }

        // ---- Build the sale; every amount comes from the catalogue ----

        var saleDate = DateTime.Now;

        var sale = new Sale
        {
            CustomerId = customer.Id,
            // SaleDate is the business date used by the dashboard and reports
            // (they bucket by the server's local calendar day), so it stays
            // local. CreatedOn is the audit timestamp and is UTC.
            SaleDate = saleDate,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = _user.Email,
            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
            ClientRequestId = request.ClientRequestId,
            TaxCalculated = true,
            PaymentTracked = true,
            IsInterState = isInterState,
            PlaceOfSupplyStateCode = placeOfSupply,
            CustomerGstin = GstStates.NormalizeGstin(customer.Gstin),
            CompanyGstin = GstStates.NormalizeGstin(company.GSTNumber),
            CompanyStateCode = companyState,
            DocumentSnapshot = BuildSnapshot(company, customer, placeOfSupply).ToJson()
        };

        var taxLines = new List<(decimal Taxable, TaxBreakup Tax)>();
        var costs = await ResolveCostsAsync(products);

        foreach (var line in lines)
        {
            var product = products[line.ProductId];
            var tax = product.TaxId.HasValue ? taxes[product.TaxId.Value] : null;
            var rate = tax?.Percentage ?? 0m;

            var unitPrice = product.SellingPrice;
            var taxable = GstCalculator.LineTaxable(line.Quantity, unitPrice);
            var breakup = GstCalculator.CalculateTax(taxable, rate, isInterState);

            taxLines.Add((taxable, breakup));

            sale.SaleItems.Add(new SaleItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                SellingPrice = unitPrice,
                Total = taxable,
                HsnCode = product.HsnCode,
                TaxName = tax?.Name,
                TaxRate = rate,
                TaxableAmount = taxable,
                CgstAmount = breakup.Cgst,
                SgstAmount = breakup.Sgst,
                IgstAmount = breakup.Igst,
                TaxAmount = breakup.Total,
                CostPrice = costs[product.Id]
            });
        }

        var totals = GstCalculator.Total(taxLines);

        sale.Subtotal = totals.Subtotal;
        sale.CgstTotal = totals.Cgst;
        sale.SgstTotal = totals.Sgst;
        sale.IgstTotal = totals.Igst;
        sale.TaxTotal = totals.Tax;
        sale.RoundOff = totals.RoundOff;
        sale.TotalAmount = totals.GrandTotal;

        var paidNow = payments.Sum(p => p.Amount);

        if (paidNow > sale.TotalAmount)
        {
            throw BusinessException.Conflict(
                $"The payments ({paidNow:0.00}) are more than the invoice total ({sale.TotalAmount:0.00}).");
        }

        // ---- Invoice number from the database-backed sequence (inside this transaction) ----

        int sequence;
        try
        {
            sequence = await _numbers.NextAsync(DocumentNumbering.InvoiceSeriesKey(saleDate));
            sale.InvoiceNumber = DocumentNumbering.Format(prefix, saleDate, sequence);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw BusinessException.Conflict(
                "The invoice number series for this financial year is exhausted.");
        }

        foreach (var payment in payments)
        {
            sale.Payments.Add(new Payment
            {
                ReceiptNumber = await NextReceiptNumberAsync(),
                Amount = payment.Amount,
                Method = payment.Method,
                Reference = string.IsNullOrWhiteSpace(payment.Reference) ? null : payment.Reference.Trim(),
                PaymentDate = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = _user.Email
            });
        }

        _context.Sales.Add(sale);

        // One stock deduction (and one ledger row) per product.
        foreach (var line in lines.OrderBy(l => l.ProductId))
        {
            await _inventoryService.AdjustStockAsync(
                line.ProductId,
                line.Quantity,
                StockTransactionType.Sale,
                sale.InvoiceNumber,
                "Sales Entry");
        }

        await _context.SaveChangesAsync();

        await RegisterWarrantiesAsync(sale, customer, company, products, lines);

        _audit.Record(
            "Sale.Create", "Sale", sale.Id.ToString(),
            $"{sale.InvoiceNumber}; customer {customer.Id}; total {sale.TotalAmount:0.00}; paid {paidNow:0.00}");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var summary = PaymentCalculator.Summarise(sale.TotalAmount, 0, paidNow, 0, true);

        return new SaleResponse
        {
            SaleId = sale.Id,
            InvoiceNumber = sale.InvoiceNumber,
            TotalAmount = sale.TotalAmount,
            PaidAmount = paidNow,
            Outstanding = summary.Outstanding,
            PaymentStatus = summary.Status.ToString()
        };
    }

    private async Task<SaleResponse?> FindByRequestIdAsync(Guid requestId)
    {
        var sale = await _context.Sales
            .AsNoTracking()
            .Where(x => x.ClientRequestId == requestId)
            .Select(x => new
            {
                x.Id,
                x.InvoiceNumber,
                x.TotalAmount,
                x.PaymentTracked,
                Paid = x.Payments.Where(p => !p.IsVoided).Sum(p => (decimal?)p.Amount) ?? 0
            })
            .FirstOrDefaultAsync();

        if (sale == null)
            return null;

        var summary = PaymentCalculator.Summarise(sale.TotalAmount, 0, sale.Paid, 0, sale.PaymentTracked);

        return new SaleResponse
        {
            SaleId = sale.Id,
            InvoiceNumber = sale.InvoiceNumber,
            TotalAmount = sale.TotalAmount,
            PaidAmount = sale.Paid,
            Outstanding = summary.Outstanding,
            PaymentStatus = summary.Status.ToString(),
            IsDuplicate = true
        };
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };

    // ------------------------------------------------------------------
    // List
    // ------------------------------------------------------------------

    public async Task<PagedResult<SaleListItemDto>> SearchAsync(SaleSearchRequest request)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.Sales.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.InvoiceNumber.Contains(search) ||
                x.Customer.Name.Contains(search));
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.Date;
            query = query.Where(x => x.SaleDate >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.SaleDate < toExclusive);
        }

        // Customers are joined without a soft-delete filter on purpose:
        // history must keep showing deleted customers.
        var rows = query.Select(x => new
        {
            x.Id,
            x.InvoiceNumber,
            x.CustomerId,
            CustomerName = x.Customer.Name,
            x.SaleDate,
            x.TotalAmount,
            x.PaymentTracked,
            ItemCount = x.SaleItems.Count,
            Paid = x.Payments.Where(p => !p.IsVoided).Sum(p => (decimal?)p.Amount) ?? 0,
            Returned = _context.SalesReturns.Where(r => r.SaleId == x.Id).Sum(r => (decimal?)r.TotalAmount) ?? 0,
            Refunded = _context.SalesReturns.Where(r => r.SaleId == x.Id).Sum(r => (decimal?)r.RefundAmount) ?? 0
        });

        if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
        {
            if (!Enum.TryParse<PaymentStatus>(request.PaymentStatus, true, out var status))
                throw new BusinessException("Unknown payment status filter.");

            // Same rules as PaymentCalculator, written so SQL Server can run them.
            rows = status switch
            {
                PaymentStatus.NotRecorded => rows.Where(r => !r.PaymentTracked),
                PaymentStatus.Settled => rows.Where(r => r.PaymentTracked && r.TotalAmount - r.Returned <= 0),
                PaymentStatus.Paid => rows.Where(r => r.PaymentTracked
                    && r.TotalAmount - r.Returned > 0
                    && (r.TotalAmount - r.Returned) - (r.Paid - r.Refunded) <= 0),
                PaymentStatus.Unpaid => rows.Where(r => r.PaymentTracked
                    && r.TotalAmount - r.Returned > 0
                    && r.Paid - r.Refunded <= 0),
                PaymentStatus.Partial => rows.Where(r => r.PaymentTracked
                    && r.TotalAmount - r.Returned > 0
                    && r.Paid - r.Refunded > 0
                    && (r.TotalAmount - r.Returned) - (r.Paid - r.Refunded) > 0),
                _ => rows
            };
        }

        int totalRecords = await rows.CountAsync();

        var page = await rows
            .OrderByDescending(x => x.SaleDate)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var data = page.Select(r =>
        {
            var summary = PaymentCalculator.Summarise(r.TotalAmount, r.Returned, r.Paid, r.Refunded, r.PaymentTracked);

            return new SaleListItemDto
            {
                Id = r.Id,
                InvoiceNumber = r.InvoiceNumber,
                CustomerId = r.CustomerId,
                CustomerName = r.CustomerName,
                SaleDate = r.SaleDate,
                TotalAmount = r.TotalAmount,
                ItemCount = r.ItemCount,
                ReturnedAmount = r.Returned,
                PaidAmount = r.Paid,
                Outstanding = r.PaymentTracked ? summary.Outstanding : 0,
                PaymentStatus = summary.Status.ToString()
            };
        }).ToList();

        return new PagedResult<SaleListItemDto>
        {
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = data
        };
    }

    // ------------------------------------------------------------------
    // Details (also the data behind the printed invoice and the PDF)
    // ------------------------------------------------------------------

    public async Task<SaleDetailDto> GetByIdAsync(int id)
    {
        var sale = await _context.Sales
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.SaleItems).ThenInclude(i => i.Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw BusinessException.NotFound("Sale not found.");

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.SaleId == id)
            .OrderBy(p => p.PaymentDate).ThenBy(p => p.Id)
            .ToListAsync();

        var returns = await _context.SalesReturns
            .AsNoTracking()
            .Where(r => r.SaleId == id)
            .Include(r => r.Items)
            .OrderBy(r => r.Id)
            .ToListAsync();

        var returnedByItem = returns
            .SelectMany(r => r.Items)
            .GroupBy(i => i.SaleItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var company = await _context.Companies.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync();
        var snapshot = InvoiceSnapshot.TryParse(sale.DocumentSnapshot);

        var paid = payments.Where(p => !p.IsVoided).Sum(p => p.Amount);
        var returnedAmount = returns.Sum(r => r.TotalAmount);
        var refunded = returns.Sum(r => r.RefundAmount);
        var summary = PaymentCalculator.Summarise(
            sale.TotalAmount, returnedAmount, paid, refunded, sale.PaymentTracked);

        return new SaleDetailDto
        {
            Id = sale.Id,
            InvoiceNumber = sale.InvoiceNumber,
            SaleDate = sale.SaleDate,
            Remarks = sale.Remarks,
            Customer = BuildCustomer(sale, snapshot),
            Company = BuildCompany(company, sale, snapshot),
            Settings = BuildSettings(company),
            Subtotal = sale.Subtotal,
            CgstTotal = sale.CgstTotal,
            SgstTotal = sale.SgstTotal,
            IgstTotal = sale.IgstTotal,
            TaxTotal = sale.TaxTotal,
            RoundOff = sale.RoundOff,
            TotalAmount = sale.TotalAmount,
            AmountInWords = AmountInWords.Rupees(sale.TotalAmount),
            TaxCalculated = sale.TaxCalculated,
            IsInterState = sale.IsInterState,
            PlaceOfSupplyStateCode = sale.PlaceOfSupplyStateCode,
            PlaceOfSupplyState = GstStates.NameOf(sale.PlaceOfSupplyStateCode),
            PaymentTracked = sale.PaymentTracked,
            PaymentStatus = summary.Status.ToString(),
            PaidAmount = paid,
            RefundedAmount = refunded,
            ReturnedAmount = returnedAmount,
            Outstanding = sale.PaymentTracked ? summary.Outstanding : 0,
            RefundDue = sale.PaymentTracked ? summary.RefundDue : 0,
            Items = sale.SaleItems
                .OrderBy(i => i.Id)
                .Select(i =>
                {
                    returnedByItem.TryGetValue(i.Id, out var returnedQty);

                    return new SaleDetailItemDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        Sku = i.Product.SKU,
                        HsnCode = i.HsnCode ?? (sale.TaxCalculated ? null : i.Product.HsnCode),
                        Quantity = i.Quantity,
                        UnitPrice = i.SellingPrice,
                        Total = i.Total,
                        TaxName = i.TaxName,
                        TaxRate = i.TaxRate,
                        CgstAmount = i.CgstAmount,
                        SgstAmount = i.SgstAmount,
                        IgstAmount = i.IgstAmount,
                        TaxAmount = i.TaxAmount,
                        LineTotal = i.TaxAmount.HasValue ? i.Total + i.TaxAmount.Value : null,
                        ReturnedQuantity = returnedQty,
                        ReturnableQuantity = i.Quantity - returnedQty
                    };
                })
                .ToList(),
            Payments = payments
                .Select(p => new SalePaymentDto
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate.AsUtc(),
                    Method = p.Method.ToString(),
                    Reference = p.Reference,
                    ReceiptNumber = p.ReceiptNumber,
                    Notes = p.Notes,
                    RecordedBy = p.CreatedBy,
                    IsVoided = p.IsVoided,
                    VoidedOn = p.VoidedOn.AsUtc(),
                    VoidReason = p.VoidReason
                })
                .ToList(),
            Returns = returns
                .Select(r => new SaleReturnSummaryDto
                {
                    Id = r.Id,
                    ReturnNumber = r.ReturnNumber,
                    ReturnDate = r.ReturnDate,
                    TotalAmount = r.TotalAmount,
                    RefundAmount = r.RefundAmount
                })
                .ToList(),
            Warranties = await LoadWarrantyLinesAsync(sale.Id)
        };
    }

    // ---- Snapshot (company + customer header as at invoice time) ----

    private static InvoiceSnapshot BuildSnapshot(Company company, Customer customer, string? placeOfSupply) => new()
    {
        Company = new SnapshotCompany
        {
            Name = company.CompanyName,
            Address = company.Address,
            City = company.City,
            State = company.State,
            StateCode = PlaceOfSupply.CompanyStateCode(company.StateCode, company.State, company.GSTNumber),
            Pincode = company.Pincode,
            Phone = company.Phone,
            Email = company.Email,
            Website = company.Website,
            Gstin = GstStates.NormalizeGstin(company.GSTNumber),
            Pan = company.PANNumber,
            LogoFile = string.IsNullOrWhiteSpace(company.LogoPath) ? null : company.LogoPath,
            BankName = company.BankName,
            AccountNumber = company.AccountNumber,
            IfscCode = company.IFSCCode,
            UpiId = company.UPIId,
            Terms = company.TermsAndConditions,
            Footer = company.FooterMessage,
            Currency = company.Currency
        },
        Customer = new SnapshotCustomer
        {
            Name = customer.Name,
            Mobile = customer.Mobile,
            Email = customer.Email,
            Address = customer.Address,
            State = customer.State ?? GstStates.NameOf(placeOfSupply),
            StateCode = placeOfSupply,
            Gstin = GstStates.NormalizeGstin(customer.Gstin)
        }
    };

    private static SaleCustomerDto BuildCustomer(Sale sale, InvoiceSnapshot? snapshot)
    {
        if (snapshot != null)
        {
            var c = snapshot.Customer;

            return new SaleCustomerDto
            {
                Id = sale.CustomerId,
                Name = c.Name,
                Mobile = c.Mobile ?? string.Empty,
                Email = c.Email,
                Address = c.Address,
                State = c.State,
                StateCode = c.StateCode,
                Gstin = c.Gstin
            };
        }

        // Invoices created before snapshots existed show the customer's current details.
        return new SaleCustomerDto
        {
            Id = sale.Customer.Id,
            Name = sale.Customer.Name,
            Mobile = sale.Customer.Mobile,
            Email = sale.Customer.Email,
            Address = sale.Customer.Address,
            State = sale.Customer.State ?? GstStates.NameOf(sale.PlaceOfSupplyStateCode),
            StateCode = sale.PlaceOfSupplyStateCode,
            // Printed GSTIN is the one frozen on the invoice (legacy invoices have none).
            Gstin = sale.TaxCalculated ? sale.CustomerGstin : sale.Customer.Gstin
        };
    }

    public static InvoiceCompanyDto BuildCompany(Company? company, Sale sale, InvoiceSnapshot? snapshot)
    {
        if (snapshot != null)
        {
            var c = snapshot.Company;

            return new InvoiceCompanyDto
            {
                Name = c.Name,
                Address = c.Address,
                City = c.City,
                State = c.State,
                StateCode = sale.CompanyStateCode ?? c.StateCode,
                Pincode = c.Pincode,
                Phone = c.Phone,
                Email = c.Email,
                Website = c.Website,
                Gstin = sale.CompanyGstin ?? c.Gstin,
                Pan = c.Pan,
                HasLogo = !string.IsNullOrWhiteSpace(c.LogoFile),
                LogoFile = c.LogoFile,
                BankName = c.BankName,
                AccountNumber = c.AccountNumber,
                IfscCode = c.IfscCode,
                UpiId = c.UpiId,
                Terms = c.Terms,
                Footer = c.Footer,
                Currency = c.Currency
            };
        }

        if (company == null)
            return new InvoiceCompanyDto();

        // Invoices created before snapshots existed show the current company details.
        return new InvoiceCompanyDto
        {
            Name = company.CompanyName,
            Address = company.Address,
            City = company.City,
            State = company.State,
            StateCode = sale.CompanyStateCode,
            Pincode = company.Pincode,
            Phone = company.Phone,
            Email = company.Email,
            Website = company.Website,
            // The GSTIN printed is the one frozen on the invoice when it exists.
            Gstin = sale.CompanyGstin ?? company.GSTNumber,
            Pan = company.PANNumber,
            HasLogo = !string.IsNullOrWhiteSpace(company.LogoPath),
            LogoFile = string.IsNullOrWhiteSpace(company.LogoPath) ? null : company.LogoPath,
            BankName = company.BankName,
            AccountNumber = company.AccountNumber,
            IfscCode = company.IFSCCode,
            UpiId = company.UPIId,
            Terms = company.TermsAndConditions,
            Footer = company.FooterMessage,
            Currency = company.Currency
        };
    }

    public static InvoiceSettingsDto BuildSettings(Company? company) => company == null
        ? new InvoiceSettingsDto()
        : new InvoiceSettingsDto
        {
            DefaultFormat = company.DefaultInvoiceFormat.ToString(),
            ThermalPaperWidthMm = company.ThermalPaperWidthMm,
            ShowBankDetails = company.ShowBankDetails,
            ShowLogo = company.ShowLogo,
            ShowTerms = company.ShowTerms,
            ShowFooter = company.ShowFooter
        };

    /// <summary>Next payment receipt number. Must run inside the caller's transaction.</summary>
    private async Task<string> NextReceiptNumberAsync()
    {
        var today = DateTime.Now;

        try
        {
            var sequence = await _numbers.NextAsync(
                DocumentNumbering.SeriesKey(DocumentNumbering.ReceiptPrefix, today));

            return DocumentNumbering.Format(DocumentNumbering.ReceiptPrefix, today, sequence);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw BusinessException.Conflict(
                "The receipt number series for this financial year is exhausted.");
        }
    }

    // ------------------------------------------------------------------
    // Payment receipts
    // ------------------------------------------------------------------

    public async Task<PaymentReceiptDto> GetReceiptAsync(int saleId, int paymentId)
    {
        // Payments made before receipts existed have no number yet. Give them one the
        // first time a receipt is requested (inside a transaction, under the sale lock).
        var needsNumber = await _context.Payments
            .AnyAsync(p => p.Id == paymentId && p.SaleId == saleId && p.ReceiptNumber == null);

        if (needsNumber)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await LockSaleAsync(saleId);

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.SaleId == saleId);

            if (payment is { ReceiptNumber: null })
            {
                payment.ReceiptNumber = await NextReceiptNumberAsync();
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }

        var sale = await _context.Sales
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == saleId)
            ?? throw BusinessException.NotFound("Sale not found.");

        var current = sale.Payments.FirstOrDefault(p => p.Id == paymentId)
            ?? throw BusinessException.NotFound("Payment not found.");

        // Return dates are local business time; payment dates are UTC.
        var paidAtLocal = DateTime.SpecifyKind(current.PaymentDate, DateTimeKind.Utc).ToLocalTime();

        var returnsBefore = await _context.SalesReturns
            .AsNoTracking()
            .Where(r => r.SaleId == saleId && r.ReturnDate <= paidAtLocal)
            .SumAsync(r => (decimal?)r.TotalAmount) ?? 0;

        var previouslyPaid = sale.Payments
            .Where(p => !p.IsVoided
                        && (p.PaymentDate < current.PaymentDate
                            || (p.PaymentDate == current.PaymentDate && p.Id < current.Id)))
            .Sum(p => p.Amount);

        var invoiceTotal = Math.Max(0, sale.TotalAmount - returnsBefore);
        var counted = current.IsVoided ? 0 : current.Amount;
        var balanceAfter = Math.Max(0, invoiceTotal - previouslyPaid - counted);
        var settles = !current.IsVoided && balanceAfter == 0;

        var company = await _context.Companies.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync();
        var snapshot = InvoiceSnapshot.TryParse(sale.DocumentSnapshot);

        return new PaymentReceiptDto
        {
            PaymentId = current.Id,
            ReceiptNumber = current.ReceiptNumber ?? string.Empty,
            PaymentDate = current.PaymentDate.AsUtc(),
            Method = current.Method.ToString(),
            Amount = current.Amount,
            Reference = current.Reference,
            Notes = current.Notes,
            ReceivedBy = current.CreatedBy,
            Status = current.IsVoided ? "Voided" : "Received",
            IsVoided = current.IsVoided,
            VoidedOn = current.VoidedOn.AsUtc(),
            VoidReason = current.VoidReason,
            SaleId = sale.Id,
            InvoiceNumber = sale.InvoiceNumber,
            InvoiceDate = sale.SaleDate,
            InvoiceTotal = invoiceTotal,
            PreviouslyPaid = previouslyPaid,
            BalanceAfter = balanceAfter,
            SettlesInvoice = settles,
            Description = current.IsVoided ? "Voided" : settles ? "Final payment" : "Part payment",
            AmountInWords = AmountInWords.Rupees(current.Amount),
            Customer = BuildCustomer(sale, snapshot),
            Company = BuildCompany(company, sale, snapshot),
            Settings = BuildSettings(company)
        };
    }
    // ------------------------------------------------------------------
    // Payments
    // ------------------------------------------------------------------

    public async Task<SaleDetailDto> RecordPaymentAsync(int saleId, RecordPaymentRequest request)
    {
        if (GstCalculator.Round(request.Amount) != request.Amount)
            throw new BusinessException("Payment amounts can have at most 2 decimal places.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        await LockSaleAsync(saleId);

        var sale = await _context.Sales
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == saleId)
            ?? throw BusinessException.NotFound("Sale not found.");

        if (!sale.PaymentTracked && !request.StartTracking)
        {
            throw BusinessException.Conflict(
                "This invoice was created before payments were recorded, so its payment history is unknown. " +
                "Confirm that payment tracking should start now to record a payment.");
        }

        var returns = await _context.SalesReturns
            .AsNoTracking()
            .Where(r => r.SaleId == saleId)
            .Select(r => new { r.TotalAmount, r.RefundAmount })
            .ToListAsync();

        var paid = sale.Payments.Where(p => !p.IsVoided).Sum(p => p.Amount);

        var summary = PaymentCalculator.Summarise(
            sale.TotalAmount,
            returns.Sum(r => r.TotalAmount),
            paid,
            returns.Sum(r => r.RefundAmount),
            paymentTracked: true);

        if (request.Amount > summary.Outstanding)
        {
            throw BusinessException.Conflict(
                summary.Outstanding == 0
                    ? "This invoice has no outstanding balance."
                    : $"The payment ({request.Amount:0.00}) is more than the outstanding balance ({summary.Outstanding:0.00}).");
        }

        var replay = sale.Payments.FirstOrDefault(p =>
            !p.IsVoided
            && p.Amount == request.Amount
            && p.Method == request.Method
            && p.CreatedBy == _user.Email
            && p.CreatedOn >= DateTime.UtcNow.AddSeconds(-15));

        if (replay != null)
        {
            await transaction.RollbackAsync();
            return await GetByIdAsync(saleId);
        }

        var payment = new Payment
        {
            SaleId = saleId,
            ReceiptNumber = await NextReceiptNumberAsync(),
            Amount = request.Amount,
            Method = request.Method,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            PaymentDate = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = _user.Email
        };

        _context.Payments.Add(payment);

        var startedTracking = !sale.PaymentTracked;
        sale.PaymentTracked = true;

        await _context.SaveChangesAsync();

        _audit.Record(
            "Payment.Record", "Sale", saleId.ToString(),
            $"{sale.InvoiceNumber}; {request.Amount:0.00} by {request.Method}" +
            (startedTracking ? "; payment tracking started for a legacy invoice" : ""));

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(saleId);
    }

    public async Task<SaleDetailDto> VoidPaymentAsync(int saleId, int paymentId, VoidPaymentRequest request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        await LockSaleAsync(saleId);

        var sale = await _context.Sales
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == saleId)
            ?? throw BusinessException.NotFound("Sale not found.");

        var payment = sale.Payments.FirstOrDefault(p => p.Id == paymentId)
            ?? throw BusinessException.NotFound("Payment not found.");

        if (payment.IsVoided)
            throw BusinessException.Conflict("This payment has already been voided.");

        var refunded = await _context.SalesReturns
            .Where(r => r.SaleId == saleId)
            .SumAsync(r => (decimal?)r.RefundAmount) ?? 0;

        var paidAfterVoid = sale.Payments.Where(p => !p.IsVoided && p.Id != paymentId).Sum(p => p.Amount);

        if (paidAfterVoid - refunded < 0)
        {
            throw BusinessException.Conflict(
                "This payment cannot be voided because refunds were already given against it.");
        }

        payment.IsVoided = true;
        payment.VoidedOn = DateTime.UtcNow;
        payment.VoidedBy = _user.Email;
        payment.VoidReason = request.Reason.Trim();

        _audit.Record(
            "Payment.Void", "Sale", saleId.ToString(),
            $"{sale.InvoiceNumber}; payment {paymentId} of {payment.Amount:0.00}; reason: {payment.VoidReason}");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(saleId);
    }

    private Task LockSaleAsync(int saleId) => SqlTransactionHelper.LockSaleAsync(_context, saleId);

    private async Task<List<WarrantyInvoiceLineDto>> LoadWarrantyLinesAsync(int saleId)
    {
        List<Domain.Entities.Warranty> rows;
        try
        {
            rows = await _context.Warranties
                .AsNoTracking()
                .Where(w => w.SaleId == saleId && !w.IsVoided)
                .OrderBy(w => w.Id)
                .ToListAsync();
        }
        catch (Exception ex) when (SqlServerErrors.IsMissingObject(ex))
        {
            return [];
        }

        return rows.Select(w => new WarrantyInvoiceLineDto
        {
            SaleItemId = w.SaleItemId,
            WarrantyNumber = w.WarrantyNumber,
            ProductName = w.ProductName,
            SerialNumber = w.SerialNumber,
            Duration = WarrantyDates.FormatDuration(w.DurationValue, w.DurationUnit),
            StartDate = w.StartDate,
            EndDate = w.EndDate
        }).ToList();
    }

    private static SaleLine MapLine(IGrouping<int, CreateSaleItemRequest> g)
    {
        var flags = g.Select(i => i.RegisterWarranty).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        if (flags.Count > 1)
            throw new BusinessException("The same product cannot both register and skip warranty on one invoice.");

        var durations = g
            .Where(i => i.WarrantyDurationValue.HasValue)
            .Select(i => (i.WarrantyDurationValue, Unit: (i.WarrantyDurationUnit ?? "").Trim().ToLowerInvariant()))
            .Distinct()
            .ToList();
        if (durations.Count > 1)
            throw new BusinessException("Use one warranty duration per product on an invoice.");

        var serials = g
            .SelectMany(i => i.SerialNumbers ?? Enumerable.Empty<string>())
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        var notes = g.Select(i => i.WarrantyNotes).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
        var start = g.Select(i => i.WarrantyStartDate).FirstOrDefault(d => d.HasValue);

        return new SaleLine(
            g.Key,
            g.Sum(i => i.Quantity),
            g.Where(i => i.UnitPrice.HasValue).Select(i => i.UnitPrice!.Value).Distinct().ToList(),
            flags.Count == 1 ? flags[0] : null,
            durations.Count == 1 ? durations[0].WarrantyDurationValue : null,
            durations.Count == 1 ? g.First(i => i.WarrantyDurationValue.HasValue).WarrantyDurationUnit : null,
            start,
            serials,
            notes);
    }

    private async Task RegisterWarrantiesAsync(
        Sale sale,
        Customer customer,
        Company company,
        Dictionary<int, Product> products,
        List<SaleLine> lines)
    {
        var walkIn = WalkInCustomer.IsWalkIn(customer.Name, customer.Mobile);
        var grocery = company.ShopType == ShopType.Grocery;

        foreach (var line in lines)
        {
            var product = products[line.ProductId];
            var hasDefault = product.WarrantyDurationValue is > 0 && product.WarrantyDurationUnit != null;
            var register = line.RegisterWarranty ?? (hasDefault && !grocery && !walkIn);

            if (!register)
                continue;

            if (walkIn)
                throw new BusinessException(
                    $"Select a customer with a real mobile number to register warranty for '{product.Name}'. Walk-in checkout cannot hold a warranty.");

            var value = line.WarrantyDurationValue ?? product.WarrantyDurationValue;
            var unitText = line.WarrantyDurationUnit ?? product.WarrantyDurationUnit?.ToString();

            if (value is null or <= 0 || string.IsNullOrWhiteSpace(unitText)
                || !Enum.TryParse<WarrantyDurationUnit>(unitText, true, out var unit)
                || !Enum.IsDefined(unit))
            {
                throw new BusinessException(
                    $"Set a warranty duration (Days, Months or Years) for '{product.Name}', or turn warranty off for this line.");
            }

            var start = (line.WarrantyStartDate ?? sale.SaleDate).Date;
            if (start > DateTime.Today)
                throw new BusinessException("Warranty start date cannot be in the future.");
            if (start < sale.SaleDate.Date.AddYears(-1))
                throw new BusinessException("Warranty start date is too far before the sale date.");

            var end = WarrantyDates.EndDate(start, value.Value, unit);

            var serials = line.SerialNumbers
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            if (serials.Count != serials.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                throw new BusinessException($"Duplicate serial numbers on '{product.Name}'.");

            if (product.RequiresSerialNumber && serials.Count != line.Quantity)
                throw new BusinessException(
                    $"'{product.Name}' requires one serial number per unit ({line.Quantity}).");

            if (serials.Count > 0 && serials.Count != line.Quantity)
                throw new BusinessException(
                    $"Enter one serial number per unit for '{product.Name}' ({line.Quantity}), or none.");

            foreach (var serial in serials)
            {
                var taken = await _context.Warranties.AnyAsync(w =>
                    w.SerialNumber == serial && !w.IsVoided);
                if (taken)
                    throw BusinessException.Conflict($"Serial number '{serial}' is already registered.");
            }

            var saleItem = sale.SaleItems.First(i => i.ProductId == line.ProductId);
            var units = serials.Count > 0
                ? serials.Select(s => (string?)s).ToList()
                : new List<string?> { null };

            foreach (var serial in units)
            {
                var sequence = await _numbers.NextAsync(DocumentNumbering.WarrantySeriesKey(sale.SaleDate));
                _context.Warranties.Add(new Domain.Entities.Warranty
                {
                    WarrantyNumber = DocumentNumbering.Format(
                        DocumentNumbering.WarrantyPrefix, sale.SaleDate, sequence),
                    SaleId = sale.Id,
                    SaleItemId = saleItem.Id,
                    ProductId = product.Id,
                    CustomerId = customer.Id,
                    InvoiceNumber = sale.InvoiceNumber,
                    ProductName = product.Name,
                    ProductSku = product.SKU,
                    CustomerName = customer.Name,
                    CustomerMobile = customer.Mobile,
                    SerialNumber = serial,
                    DurationValue = value.Value,
                    DurationUnit = unit,
                    StartDate = start,
                    EndDate = end,
                    Notes = string.IsNullOrWhiteSpace(line.WarrantyNotes) ? null : line.WarrantyNotes.Trim(),
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = _user.Email
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Cost of the goods being sold, captured before stock is reduced.
    /// Catalogue purchase price first (updated on each purchase / opening entry);
    /// if that is unset, the last purchase line for the product. Zero is unknown.
    /// </summary>
    private async Task<Dictionary<int, decimal?>> ResolveCostsAsync(Dictionary<int, Product> products)
    {
        var result = new Dictionary<int, decimal?>();
        var missing = new List<int>();

        foreach (var product in products.Values)
        {
            var snap = CostSnapshot.FromCatalogue(product.PurchasePrice);
            if (snap.HasValue)
                result[product.Id] = snap;
            else
                missing.Add(product.Id);
        }

        if (missing.Count == 0)
            return result;

        var rows = await _context.PurchaseItems
            .AsNoTracking()
            .Where(i => missing.Contains(i.ProductId) && i.PurchasePrice > 0)
            .Select(i => new { i.ProductId, i.PurchasePrice, i.Purchase.PurchaseDate, i.Id })
            .ToListAsync();

        foreach (var id in missing)
        {
            var last = rows
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.PurchaseDate)
                .ThenByDescending(r => r.Id)
                .FirstOrDefault();

            result[id] = last is null ? null : GstCalculator.Round(last.PurchasePrice);
        }

        return result;
    }

    private sealed record SaleLine(
        int ProductId,
        int Quantity,
        List<decimal> EchoedPrices,
        bool? RegisterWarranty,
        int? WarrantyDurationValue,
        string? WarrantyDurationUnit,
        DateTime? WarrantyStartDate,
        List<string> SerialNumbers,
        string? WarrantyNotes);
}

using Billing.API.Services;
using Billing.Application.DTOs.Sales;
using Billing.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Billing.Tests;

/// <summary>
/// Renders every invoice and receipt layout from sample data and checks the page geometry.
/// Set INVOICE_PREVIEW_DIR to also write the pages as PNG files for a visual check.
/// </summary>
public class InvoiceDocumentTests
{
    private static readonly InvoicePdfService Invoices = new();
    private static readonly ReceiptPdfService Receipts = new();

    private static SaleDetailDto Sample(int lines = 3, bool intra = true, bool showBank = false, int payments = 1)
    {
        var sale = new SaleDetailDto
        {
            Id = 1,
            InvoiceNumber = "INV-2627-000123",
            SaleDate = new DateTime(2026, 10, 28),
            Customer = new SaleCustomerDto
            {
                Id = 1, Name = "Anita Sharma", Mobile = "9876543210", Address = "12 MG Road, Hyderabad 500001",
                State = "Telangana", StateCode = "36", Gstin = "36ABCDE1234F1Z5"
            },
            Company = new InvoiceCompanyDto
            {
                Name = "Sri Lakshmi Electronics", Address = "Shop 4, Main Bazaar", City = "Hyderabad",
                State = "Telangana", StateCode = "36", Pincode = "500001", Phone = "9000000000",
                Email = "shop@example.com", Gstin = "36AAAAA0000A1Z5", Pan = "AAAAA0000A",
                BankName = "Demo Bank", AccountNumber = "123456789012", IfscCode = "DEMO0001234", UpiId = "shop@demo",
                Terms = "Goods once sold will not be taken back. Warranty as per manufacturer.",
                Footer = "Thank you for shopping with us!"
            },
            Settings = new InvoiceSettingsDto { ShowBankDetails = showBank },
            TaxCalculated = true,
            IsInterState = !intra,
            PlaceOfSupplyStateCode = intra ? "36" : "27",
            PlaceOfSupplyState = intra ? "Telangana" : "Maharashtra",
            PaymentTracked = true
        };

        decimal taxable = 0, cgst = 0, sgst = 0, igst = 0;

        for (var i = 1; i <= lines; i++)
        {
            var qty = i % 3 + 1;
            var price = 100m * i;
            var lineTaxable = qty * price;
            var rate = i % 2 == 0 ? 18m : 12m;
            var tax = Math.Round(lineTaxable * rate / 100, 2);
            var half = Math.Round(lineTaxable * rate / 200, 2);

            sale.Items.Add(new SaleDetailItemDto
            {
                Id = i, ProductId = i, ProductName = $"Sample product number {i} with a fairly long descriptive name",
                Sku = $"SKU-{i:000}", HsnCode = "8517", Quantity = qty, UnitPrice = price, Total = lineTaxable,
                TaxRate = rate, TaxAmount = intra ? half * 2 : tax,
                CgstAmount = intra ? half : 0, SgstAmount = intra ? half : 0, IgstAmount = intra ? 0 : tax,
                LineTotal = lineTaxable + (intra ? half * 2 : tax), ReturnableQuantity = qty
            });

            taxable += lineTaxable;
            cgst += intra ? half : 0;
            sgst += intra ? half : 0;
            igst += intra ? 0 : tax;
        }

        sale.Subtotal = taxable;
        sale.CgstTotal = cgst;
        sale.SgstTotal = sgst;
        sale.IgstTotal = igst;
        sale.TaxTotal = cgst + sgst + igst;
        var exact = taxable + sale.TaxTotal;
        sale.TotalAmount = Math.Round(exact, 0);
        sale.RoundOff = sale.TotalAmount - exact;
        sale.AmountInWords = "Rupees Test Only";

        for (var p = 1; p <= payments; p++)
        {
            sale.Payments.Add(new SalePaymentDto
            {
                Id = p, Amount = Math.Round(sale.TotalAmount / 3, 0), Method = p % 2 == 0 ? "Upi" : "Cash",
                PaymentDate = DateTime.UtcNow, ReceiptNumber = $"RCT-2627-{p:000000}"
            });
        }

        sale.PaidAmount = sale.Payments.Sum(x => x.Amount);
        sale.Outstanding = sale.TotalAmount - sale.PaidAmount;
        sale.PaymentStatus = sale.Outstanding > 0 ? "Partial" : "Paid";

        return sale;
    }

    private static (double Width, double Height, int Pages) Measure(IDocument document, string name)
    {
        var images = document.GenerateImages(new ImageGenerationSettings { RasterDpi = 72 }).ToList();
        Assert.NotEmpty(images);

        var png = images[0];
        Assert.Equal(0x89, png[0]);
        var width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        var height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];

        var folder = Environment.GetEnvironmentVariable("INVOICE_PREVIEW_DIR");
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
            for (var i = 0; i < images.Count; i++)
                File.WriteAllBytes(Path.Combine(folder, $"{name}-p{i + 1}.png"), images[i]);
        }

        return (width, height, images.Count);
    }

    [Fact]
    public void A4_invoice_is_a_single_a4_page_for_a_short_invoice()
    {
        var (w, h, pages) = Measure(Invoices.Compose(Sample(), InvoiceFormat.A4, 80, null), "a4-short");

        Assert.Equal(595, w, 2);
        Assert.Equal(842, h, 2);
        Assert.Equal(1, pages);
    }

    [Fact]
    public void A4_invoice_paginates_a_long_invoice()
    {
        var doc = Invoices.Compose(Sample(lines: 70, payments: 2), InvoiceFormat.A4, 80, null);
        var (w, h, pages) = Measure(doc, "a4-long");

        Assert.Equal(595, w, 2);
        Assert.Equal(842, h, 2);
        Assert.True(pages >= 2, $"expected several pages, got {pages}");
    }

    [Fact]
    public void A4_inter_state_invoice_renders()
    {
        var (_, _, pages) = Measure(Invoices.Compose(Sample(intra: false), InvoiceFormat.A4, 80, null), "a4-igst");
        Assert.Equal(1, pages);
    }

    [Fact]
    public void Compact_invoice_uses_its_own_a5_page()
    {
        var (w, h, pages) = Measure(Invoices.Compose(Sample(), InvoiceFormat.Compact, 80, null), "compact");

        Assert.Equal(420, w, 2);
        Assert.Equal(595, h, 2);
        Assert.Equal(1, pages);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(58)]
    public void Thermal_receipt_is_a_narrow_single_continuous_page(int widthMm)
    {
        var (w, h, pages) = Measure(
            Invoices.Compose(Sample(lines: 8), InvoiceFormat.Thermal, widthMm, null), $"thermal{widthMm}");

        // Images are whole pixels at 72 dpi, so allow one point of rounding.
        Assert.InRange(w, widthMm * 72.0 / 25.4 - 1, widthMm * 72.0 / 25.4 + 1);
        Assert.Equal(1, pages);
        Assert.True(h < 842, "a thermal receipt must not be forced to A4 height");
    }

    [Fact]
    public void Showing_bank_details_changes_the_output()
    {
        var hidden = Invoices.Compose(Sample(showBank: false), InvoiceFormat.A4, 80, null)
            .GenerateImages(new ImageGenerationSettings { RasterDpi = 72 }).First();
        var shown = Invoices.Compose(Sample(showBank: true), InvoiceFormat.A4, 80, null)
            .GenerateImages(new ImageGenerationSettings { RasterDpi = 72 }).First();

        Assert.False(hidden.SequenceEqual(shown));
    }

    [Fact]
    public void Pdf_bytes_are_a_valid_pdf_for_every_format()
    {
        foreach (var format in Enum.GetValues<InvoiceFormat>())
        {
            var bytes = Invoices.Generate(Sample(), format, 80, null);

            Assert.True(bytes.Length > 1000);
            Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
        }
    }

    [Fact]
    public void Missing_optional_company_details_do_not_break_the_pdf()
    {
        var sale = Sample();
        sale.Company = new InvoiceCompanyDto { Name = "Bare Shop" };
        sale.Customer.Address = null;
        sale.Customer.Gstin = null;
        sale.Customer.State = null;

        foreach (var format in Enum.GetValues<InvoiceFormat>())
            Assert.NotEmpty(Invoices.Generate(sale, format, 58, null));
    }

    [Fact]
    public void Legacy_invoice_without_tax_or_payment_tracking_renders()
    {
        var sale = Sample();
        sale.TaxCalculated = false;
        sale.PaymentTracked = false;
        sale.PaymentStatus = "NotRecorded";

        foreach (var format in Enum.GetValues<InvoiceFormat>())
            Assert.NotEmpty(Invoices.Generate(sale, format, 80, null));
    }

    private static PaymentReceiptDto SampleReceipt(bool settles = false, bool voided = false) => new()
    {
        PaymentId = 7, ReceiptNumber = "RCT-2627-000007", PaymentDate = DateTime.UtcNow, Method = "Upi",
        Amount = 500, Reference = "UTR123456", ReceivedBy = "admin@example.com",
        Status = voided ? "Voided" : "Received", IsVoided = voided, VoidReason = voided ? "Entered twice" : null,
        VoidedOn = voided ? DateTime.UtcNow : null,
        SaleId = 1, InvoiceNumber = "INV-2627-000123", InvoiceDate = new DateTime(2026, 10, 28),
        InvoiceTotal = 1500, PreviouslyPaid = settles ? 1000 : 400, BalanceAfter = settles ? 0 : 600,
        SettlesInvoice = settles && !voided, AmountInWords = "Rupees Five Hundred Only",
        Customer = new SaleCustomerDto { Name = "Anita Sharma" },
        Company = new InvoiceCompanyDto { Name = "Sri Lakshmi Electronics", Phone = "9000000000", Footer = "Thank you!" },
        Settings = new InvoiceSettingsDto()
    };

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Receipt_renders_on_a5_and_thermal(bool settles, bool voided)
    {
        var tag = voided ? "voided" : settles ? "final" : "part";

        var (w, h, pages) = Measure(Receipts.Compose(SampleReceipt(settles, voided), InvoiceFormat.A4, 80, null), $"receipt-{tag}");
        Assert.Equal(420, w, 2);
        Assert.Equal(595, h, 2);
        Assert.Equal(1, pages);

        var (tw, _, tPages) = Measure(Receipts.Compose(SampleReceipt(settles, voided), InvoiceFormat.Thermal, 58, null), $"receipt-{tag}-58");
        Assert.InRange(tw, 58 * 72.0 / 25.4 - 1, 58 * 72.0 / 25.4 + 1);
        Assert.Equal(1, tPages);
    }
}
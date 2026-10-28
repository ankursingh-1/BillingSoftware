using Billing.Application.DTOs.Sales;
using Billing.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static Billing.API.Services.PdfParts;

namespace Billing.API.Services;

/// <summary>
/// Renders a saved sale as a PDF in one of three independent layouts: a full A4 tax invoice,
/// a compact counter invoice (A5 page) and a narrow thermal roll receipt (58 or 80 mm).
/// The figures come straight from the saved sale (SaleDetailDto); nothing is recalculated here.
/// </summary>
public class InvoicePdfService
{
    public byte[] Generate(SaleDetailDto sale, InvoiceFormat format, int thermalWidthMm, byte[]? logo) =>
        Compose(sale, format, thermalWidthMm, logo).GeneratePdf();

    /// <summary>The document model (also used to render preview images in tests).</summary>
    public IDocument Compose(SaleDetailDto sale, InvoiceFormat format, int thermalWidthMm, byte[]? logo)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var view = new InvoiceView(sale, logo);

        return format switch
        {
            InvoiceFormat.Compact => Compact(view),
            InvoiceFormat.Thermal => Thermal(view, thermalWidthMm == 58 ? 58 : 80),
            _ => A4(view)
        };
    }

    // ------------------------------------------------------------------
    // Shared view model: decides what is shown, so layouts never print blanks.
    // ------------------------------------------------------------------

    private sealed class InvoiceView
    {
        public InvoiceView(SaleDetailDto sale, byte[]? logo)
        {
            Sale = sale;
            Company = sale.Company;
            Settings = sale.Settings;
            Logo = Settings.ShowLogo && logo is { Length: > 0 } ? logo : null;
            ShowTax = sale.TaxCalculated;
            Title = ShowTax && !string.IsNullOrWhiteSpace(Company.Gstin) ? "TAX INVOICE" : "INVOICE";
            BankLines = Settings.ShowBankDetails ? BankLinesOf(Company).ToList() : new List<string>();
            Terms = Settings.ShowTerms && !string.IsNullOrWhiteSpace(Company.Terms) ? Company.Terms!.Trim() : null;
            Footer = Settings.ShowFooter && !string.IsNullOrWhiteSpace(Company.Footer) ? Company.Footer!.Trim() : null;
            Status = StatusStyle(sale.PaymentStatus);
            Payments = sale.Payments.Where(p => !p.IsVoided).ToList();
        }

        public SaleDetailDto Sale { get; }
        public InvoiceCompanyDto Company { get; }
        public InvoiceSettingsDto Settings { get; }
        public byte[]? Logo { get; }
        public bool ShowTax { get; }
        public string Title { get; }
        public List<string> BankLines { get; }
        public string? Terms { get; }
        public string? Footer { get; }
        public (string Label, string Colour) Status { get; }
        public List<SalePaymentDto> Payments { get; }
        public bool Tracked => Sale.PaymentTracked;
        public bool Intra => !Sale.IsInterState;

        private static IEnumerable<string> BankLinesOf(InvoiceCompanyDto c) => PdfParts.BankLines(c);
    }

    private static void T(IContainer cell, string text, float size = 8, bool bold = false, bool right = false,
        bool center = false, string? colour = null, bool wrapAnywhere = false)
    {
        var aligned = right ? cell.AlignRight() : center ? cell.AlignCenter() : cell;
        aligned.Text(t =>
        {
            var span = t.Span(text).FontSize(size);
            if (bold) span.Bold();
            if (colour != null) span.FontColor(colour);
            if (wrapAnywhere) t.DefaultTextStyle(s => s.WrapAnywhere());
        });
    }

    // ------------------------------------------------------------------
    // A4 tax invoice
    // ------------------------------------------------------------------

    private static IDocument A4(InvoiceView v)
    {
        var sale = v.Sale;
        var company = v.Company;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(14, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Ink));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        if (v.Logo != null)
                        {
                            row.ConstantItem(Mm(26)).Height(Mm(20)).Image(v.Logo).FitArea();
                            row.ConstantItem(Mm(4));
                        }

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(company.Name).FontSize(16).Bold().FontColor(Accent);

                            foreach (var line in AddressLines(company))
                                c.Item().Text(line).FontColor(Muted);

                            if (ContactLine(company) is { } contact)
                                c.Item().Text(contact).FontColor(Muted);

                            if (TaxIdLine(company) is { } ids)
                                c.Item().Text(ids).SemiBold();
                        });

                        row.ConstantItem(Mm(58)).Column(c =>
                        {
                            c.Item().AlignRight().Text(v.Title).FontSize(15).Bold().FontColor(Accent);
                            c.Item().AlignRight().Text($"Invoice No: {sale.InvoiceNumber}").SemiBold();
                            c.Item().AlignRight().Text($"Date: {sale.SaleDate:dd-MMM-yyyy}");

                            if (v.Tracked)
                                c.Item().AlignRight().Text(v.Status.Label).Bold().FontSize(9)
                                    .FontColor(v.Status.Colour);
                        });
                    });

                    header.Item().PaddingTop(5).LineHorizontal(1.2f).LineColor(Accent);
                });

                page.Content().PaddingTop(8).Column(content =>
                {
                    content.Spacing(8);

                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Border(0.5f).BorderColor(Rule).Padding(6).Column(c =>
                        {
                            c.Item().Text("BILL TO").FontSize(7.5f).Bold().FontColor(Muted);
                            c.Item().Text(sale.Customer.Name).Bold().FontSize(10);

                            if (!string.IsNullOrWhiteSpace(sale.Customer.Address))
                                c.Item().Text(sale.Customer.Address);

                            if (!string.IsNullOrWhiteSpace(sale.Customer.Mobile))
                                c.Item().Text($"Mobile: {sale.Customer.Mobile}");

                            if (!string.IsNullOrWhiteSpace(sale.Customer.Gstin))
                                c.Item().Text($"GSTIN: {sale.Customer.Gstin}");

                            if (!string.IsNullOrWhiteSpace(sale.Customer.State))
                                c.Item().Text($"State: {sale.Customer.State}");
                        });

                        row.ConstantItem(Mm(4));

                        row.RelativeItem().Border(0.5f).BorderColor(Rule).Padding(6).Column(c =>
                        {
                            c.Item().Text("SUPPLY DETAILS").FontSize(7.5f).Bold().FontColor(Muted);
                            c.Item().Text($"Invoice No: {sale.InvoiceNumber}");
                            c.Item().Text($"Invoice Date: {sale.SaleDate:dd-MMM-yyyy}");

                            if (v.ShowTax && sale.PlaceOfSupplyStateCode != null)
                            {
                                c.Item().Text(
                                    $"Place of supply: {sale.PlaceOfSupplyState} ({sale.PlaceOfSupplyStateCode})");
                                c.Item().Text(sale.IsInterState
                                    ? "Inter-state supply (IGST)"
                                    : "Intra-state supply (CGST + SGST)").FontColor(Muted);
                            }

                            if (!string.IsNullOrWhiteSpace(sale.Remarks))
                                c.Item().Text($"Note: {sale.Remarks}").FontColor(Muted);
                        });
                    });

                    content.Item().Element(e => A4Items(e, v));

                    content.Item().ShowEntire().Row(row =>
                    {
                        row.RelativeItem().PaddingRight(10).Column(left =>
                        {
                            left.Spacing(5);

                            left.Item().Column(c =>
                            {
                                c.Item().Text("Amount in words").FontSize(7.5f).Bold().FontColor(Muted);
                                c.Item().Text(sale.AmountInWords).Italic();
                            });

                            if (v.ShowTax && sale.Items.Any(i => i.TaxRate.HasValue))
                                left.Item().Element(e => A4TaxSummary(e, v));

                            if (!v.ShowTax)
                                left.Item().Text(
                                    "Tax was not recorded for this invoice (created before GST details were captured).")
                                    .FontSize(8).FontColor(Muted);
                        });

                        row.ConstantItem(Mm(78)).Element(e => A4Totals(e, v));
                    });

                    if (v.BankLines.Count > 0)
                        content.Item().ShowEntire().Column(c =>
                        {
                            c.Item().Text("PAYMENT DETAILS").FontSize(7.5f).Bold().FontColor(Muted);
                            foreach (var line in v.BankLines)
                                c.Item().Text(line);
                        });

                    if (v.Terms != null)
                        content.Item().ShowEntire().Column(c =>
                        {
                            c.Item().Text("TERMS & CONDITIONS").FontSize(7.5f).Bold().FontColor(Muted);
                            c.Item().Text(v.Terms).FontSize(8);
                        });

                    content.Item().ShowEntire().AlignRight().Width(Mm(60)).PaddingTop(18).Column(c =>
                    {
                        c.Item().LineHorizontal(0.5f).LineColor(Ink);
                        c.Item().AlignCenter().Text($"For {company.Name}").FontSize(8);
                        c.Item().AlignCenter().Text("Authorised signatory").FontSize(7.5f).FontColor(Muted);
                    });
                });

                page.Footer().Column(f =>
                {
                    f.Item().LineHorizontal(0.5f).LineColor(Rule);
                    f.Item().PaddingTop(3).Row(r =>
                    {
                        r.RelativeItem().Text(v.Footer ?? string.Empty).FontColor(Muted).FontSize(8);
                        r.ConstantItem(Mm(62)).AlignRight().Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(8).FontColor(Muted));
                            t.Span($"{sale.InvoiceNumber}  |  Page ");
                            t.CurrentPageNumber();
                            t.Span(" of ");
                            t.TotalPages();
                        });
                    });
                });
            });
        });
    }

    private static void A4Items(IContainer container, InvoiceView v)
    {
        var sale = v.Sale;
        var tax = v.ShowTax;
        var intra = v.Intra;

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(Mm(7));          // #
                c.RelativeColumn(5);              // Description
                if (tax) c.ConstantColumn(Mm(14)); // HSN
                c.ConstantColumn(Mm(9));          // Qty
                c.ConstantColumn(Mm(18));         // Unit price
                if (tax)
                {
                    c.ConstantColumn(Mm(19));     // Taxable
                    c.ConstantColumn(Mm(10));     // GST %
                    if (intra)
                    {
                        c.ConstantColumn(Mm(15)); // CGST
                        c.ConstantColumn(Mm(15)); // SGST
                    }
                    else
                    {
                        c.ConstantColumn(Mm(17)); // IGST
                    }
                }
                c.ConstantColumn(Mm(21));         // Total
            });

            table.Header(h =>
            {
                T(h.Cell().GridCell(true), "#", 7.5f, true);
                T(h.Cell().GridCell(true), "Description", 7.5f, true);
                if (tax) T(h.Cell().GridCell(true), "HSN", 7.5f, true);
                T(h.Cell().GridCell(true), "Qty", 7.5f, true, right: true);
                T(h.Cell().GridCell(true), "Unit price", 7.5f, true, right: true);
                if (tax)
                {
                    T(h.Cell().GridCell(true), "Taxable", 7.5f, true, right: true);
                    T(h.Cell().GridCell(true), "GST %", 7.5f, true, right: true);
                    if (intra)
                    {
                        T(h.Cell().GridCell(true), "CGST", 7.5f, true, right: true);
                        T(h.Cell().GridCell(true), "SGST", 7.5f, true, right: true);
                    }
                    else
                    {
                        T(h.Cell().GridCell(true), "IGST", 7.5f, true, right: true);
                    }
                }
                T(h.Cell().GridCell(true), "Amount", 7.5f, true, right: true);
            });

            var n = 1;
            foreach (var item in sale.Items)
            {
                T(table.Cell().GridCell(), (n++).ToString(), 8);

                table.Cell().GridCell().Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.WrapAnywhere());
                        t.Span(item.ProductName).FontSize(8.5f).SemiBold();
                    });

                    if (!string.IsNullOrWhiteSpace(item.Sku))
                        c.Item().Text($"SKU: {item.Sku}").FontSize(7).FontColor(Muted);
                });

                if (tax) T(table.Cell().GridCell(), item.HsnCode ?? "", 8);
                T(table.Cell().GridCell(), item.Quantity.ToString(), 8, right: true);
                T(table.Cell().GridCell(), Money(item.UnitPrice), 8, right: true);

                if (tax)
                {
                    T(table.Cell().GridCell(), Money(item.Total), 8, right: true);
                    T(table.Cell().GridCell(), $"{item.TaxRate ?? 0:0.##}", 8, right: true);
                    if (intra)
                    {
                        T(table.Cell().GridCell(), Money(item.CgstAmount ?? 0), 8, right: true);
                        T(table.Cell().GridCell(), Money(item.SgstAmount ?? 0), 8, right: true);
                    }
                    else
                    {
                        T(table.Cell().GridCell(), Money(item.IgstAmount ?? 0), 8, right: true);
                    }
                }

                T(table.Cell().GridCell(), Money(item.LineTotal ?? item.Total), 8, bold: true, right: true);
            }
        });
    }

    private static void A4TaxSummary(IContainer container, InvoiceView v)
    {
        var groups = v.Sale.Items
            .Where(i => i.TaxRate.HasValue)
            .GroupBy(i => i.TaxRate!.Value)
            .OrderBy(g => g.Key)
            .ToList();

        container.Column(c =>
        {
            c.Item().Text("TAX SUMMARY").FontSize(7.5f).Bold().FontColor(Muted);

            c.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(Mm(12));
                    cols.RelativeColumn();
                    if (v.Intra)
                    {
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    }
                    else
                    {
                        cols.RelativeColumn();
                    }
                });

                table.Header(h =>
                {
                    T(h.Cell().GridCell(true), "Rate", 7, true);
                    T(h.Cell().GridCell(true), "Taxable", 7, true, right: true);
                    if (v.Intra)
                    {
                        T(h.Cell().GridCell(true), "CGST", 7, true, right: true);
                        T(h.Cell().GridCell(true), "SGST", 7, true, right: true);
                    }
                    else
                    {
                        T(h.Cell().GridCell(true), "IGST", 7, true, right: true);
                    }
                });

                foreach (var g in groups)
                {
                    T(table.Cell().GridCell(), $"{g.Key:0.##}%", 7.5f);
                    T(table.Cell().GridCell(), Money(g.Sum(i => i.Total)), 7.5f, right: true);
                    if (v.Intra)
                    {
                        T(table.Cell().GridCell(), Money(g.Sum(i => i.CgstAmount ?? 0)), 7.5f, right: true);
                        T(table.Cell().GridCell(), Money(g.Sum(i => i.SgstAmount ?? 0)), 7.5f, right: true);
                    }
                    else
                    {
                        T(table.Cell().GridCell(), Money(g.Sum(i => i.IgstAmount ?? 0)), 7.5f, right: true);
                    }
                }
            });
        });
    }

    private static void A4Totals(IContainer container, InvoiceView v)
    {
        var sale = v.Sale;

        void Row(ColumnDescriptor c, string label, string value, bool muted = true) =>
            c.Item().PaddingVertical(1.5f).Row(r =>
            {
                r.RelativeItem().Text(label).FontColor(muted ? Muted : Ink);
                r.ConstantItem(Mm(32)).AlignRight().Text(value);
            });

        container.Border(0.5f).BorderColor(Rule).Padding(6).Column(c =>
        {
            Row(c, v.ShowTax ? "Taxable value" : "Subtotal", Rs(sale.Subtotal));

            if (v.ShowTax)
            {
                if (v.Intra)
                {
                    Row(c, "CGST", Rs(sale.CgstTotal));
                    Row(c, "SGST", Rs(sale.SgstTotal));
                }
                else
                {
                    Row(c, "IGST", Rs(sale.IgstTotal));
                }

                Row(c, "Total tax", Rs(sale.TaxTotal));
            }

            if (sale.RoundOff != 0)
                Row(c, "Round off", Rs(sale.RoundOff));

            c.Item().PaddingTop(3).LineHorizontal(1).LineColor(Ink);

            c.Item().PaddingVertical(3).Row(r =>
            {
                r.RelativeItem().Text("GRAND TOTAL").Bold().FontSize(10.5f);
                r.ConstantItem(Mm(32)).AlignRight().Text(Rs(sale.TotalAmount)).Bold().FontSize(10.5f);
            });

            if (sale.ReturnedAmount > 0)
                Row(c, "Returned", $"- {Rs(sale.ReturnedAmount)}");

            if (v.Tracked)
            {
                c.Item().LineHorizontal(0.5f).LineColor(Rule);
                Row(c, "Paid", Rs(sale.PaidAmount - sale.RefundedAmount), muted: false);

                c.Item().PaddingVertical(1.5f).Row(r =>
                {
                    r.RelativeItem().Text("Balance due").Bold();
                    r.ConstantItem(Mm(32)).AlignRight().Text(Rs(sale.Outstanding)).Bold()
                        .FontColor(sale.Outstanding > 0 ? "#b91c1c" : "#15803d");
                });

                if (sale.RefundDue > 0)
                    Row(c, "Refund due to customer", Rs(sale.RefundDue), muted: false);

                if (v.Payments.Count > 0)
                    c.Item().PaddingTop(2).Text(
                        "Received by " + string.Join(", ",
                            v.Payments.GroupBy(p => p.Method)
                                .Select(g => $"{MethodLabel(g.Key)} {Money(g.Sum(p => p.Amount))}")))
                        .FontSize(7.5f).FontColor(Muted);
            }
            else
            {
                c.Item().PaddingTop(2).Text("Payment status: not recorded").FontColor(Muted).FontSize(8);
            }
        });
    }

    // ------------------------------------------------------------------
    // Compact retail invoice (A5 page, its own layout)
    // ------------------------------------------------------------------

    private static IDocument Compact(InvoiceView v)
    {
        var sale = v.Sale;
        var company = v.Company;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(8, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Ink));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        if (v.Logo != null)
                        {
                            row.ConstantItem(Mm(16)).Height(Mm(13)).Image(v.Logo).FitArea();
                            row.ConstantItem(Mm(3));
                        }

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(company.Name).FontSize(12).Bold().FontColor(Accent);

                            var place = Join(", ", AddressLines(company).ToArray());
                            if (place.Length > 0)
                                c.Item().Text(place).FontSize(7.5f).FontColor(Muted);

                            var small = Join("  |  ", Prefixed("Ph", company.Phone), Prefixed("GSTIN", company.Gstin));
                            if (small.Length > 0)
                                c.Item().Text(small).FontSize(7.5f).FontColor(Muted);
                        });

                        row.ConstantItem(Mm(38)).Column(c =>
                        {
                            c.Item().AlignRight().Text(v.Title).Bold().FontSize(10).FontColor(Accent);
                            c.Item().AlignRight().Text(sale.InvoiceNumber).Bold();
                            c.Item().AlignRight().Text($"{sale.SaleDate:dd-MMM-yyyy}").FontColor(Muted);
                        });
                    });

                    header.Item().PaddingTop(3).LineHorizontal(1).LineColor(Accent);
                });

                page.Content().PaddingTop(4).Column(content =>
                {
                    content.Spacing(4);

                    content.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("Customer: ").FontColor(Muted);
                            t.Span(sale.Customer.Name).Bold();
                            if (!string.IsNullOrWhiteSpace(sale.Customer.Mobile))
                                t.Span($"  ({sale.Customer.Mobile})").FontColor(Muted);
                        });

                        if (!string.IsNullOrWhiteSpace(sale.Customer.Gstin))
                            r.ConstantItem(Mm(50)).AlignRight().Text($"GSTIN: {sale.Customer.Gstin}").FontSize(7.5f);
                    });

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);
                            c.ConstantColumn(Mm(9));
                            c.ConstantColumn(Mm(16));
                            if (v.ShowTax) c.ConstantColumn(Mm(9));
                            c.ConstantColumn(Mm(19));
                        });

                        table.Header(h =>
                        {
                            T(h.Cell().GridCell(true), "Item", 7.5f, true);
                            T(h.Cell().GridCell(true), "Qty", 7.5f, true, right: true);
                            T(h.Cell().GridCell(true), "Rate", 7.5f, true, right: true);
                            if (v.ShowTax) T(h.Cell().GridCell(true), "GST", 7.5f, true, right: true);
                            T(h.Cell().GridCell(true), "Amount", 7.5f, true, right: true);
                        });

                        foreach (var item in sale.Items)
                        {
                            table.Cell().GridCell().Column(c =>
                            {
                                c.Item().Text(t =>
                                {
                                    t.DefaultTextStyle(s => s.WrapAnywhere());
                                    t.Span(item.ProductName).FontSize(8.5f).SemiBold();
                                });

                                var meta = Join("  ", item.Sku, v.ShowTax && !string.IsNullOrWhiteSpace(item.HsnCode)
                                    ? $"HSN {item.HsnCode}" : null);
                                if (meta.Length > 0)
                                    c.Item().Text(meta).FontSize(6.5f).FontColor(Muted);
                            });

                            T(table.Cell().GridCell(), item.Quantity.ToString(), 8.5f, right: true);
                            T(table.Cell().GridCell(), Money(item.UnitPrice), 8.5f, right: true);
                            if (v.ShowTax) T(table.Cell().GridCell(), $"{item.TaxRate ?? 0:0.##}%", 8, right: true);
                            T(table.Cell().GridCell(), Money(item.LineTotal ?? item.Total), 8.5f, bold: true, right: true);
                        }
                    });

                    content.Item().ShowEntire().Row(row =>
                    {
                        row.RelativeItem().PaddingRight(6).Column(left =>
                        {
                            left.Spacing(2);

                            if (v.Tracked)
                            {
                                left.Item().Text(v.Status.Label).Bold().FontColor(v.Status.Colour);

                                if (v.Payments.Count > 0)
                                    left.Item().Text(string.Join(", ", v.Payments.GroupBy(p => p.Method)
                                        .Select(g => $"{MethodLabel(g.Key)} {Money(g.Sum(p => p.Amount))}")))
                                        .FontSize(7.5f).FontColor(Muted);
                            }
                            else
                            {
                                left.Item().Text("Payment not recorded").FontColor(Muted);
                            }

                            if (!v.ShowTax)
                                left.Item().Text("Tax not recorded for this invoice.").FontSize(7).FontColor(Muted);
                        });

                        row.ConstantItem(Mm(62)).Column(c =>
                        {
                            void Line(string label, string value) => c.Item().Row(r =>
                            {
                                r.RelativeItem().Text(label).FontColor(Muted);
                                r.ConstantItem(Mm(28)).AlignRight().Text(value);
                            });

                            Line(v.ShowTax ? "Taxable" : "Subtotal", Rs(sale.Subtotal));

                            if (v.ShowTax)
                            {
                                if (v.Intra)
                                {
                                    Line("CGST", Rs(sale.CgstTotal));
                                    Line("SGST", Rs(sale.SgstTotal));
                                }
                                else
                                {
                                    Line("IGST", Rs(sale.IgstTotal));
                                }
                            }

                            if (sale.RoundOff != 0)
                                Line("Round off", Rs(sale.RoundOff));

                            c.Item().PaddingTop(3).Background(Accent).Padding(4).Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL").Bold().FontSize(11).FontColor(Colors.White);
                                r.ConstantItem(Mm(32)).AlignRight().Text(Rs(sale.TotalAmount))
                                    .Bold().FontSize(11).FontColor(Colors.White);
                            });

                            if (v.Tracked)
                            {
                                Line("Paid", Rs(sale.PaidAmount - sale.RefundedAmount));

                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Balance").Bold();
                                    r.ConstantItem(Mm(28)).AlignRight().Text(Rs(sale.Outstanding)).Bold()
                                        .FontColor(sale.Outstanding > 0 ? "#b91c1c" : "#15803d");
                                });
                            }
                        });
                    });

                    content.Item().Text(sale.AmountInWords).Italic().FontSize(7.5f).FontColor(Muted);

                    if (v.BankLines.Count > 0)
                        content.Item().Text(string.Join("   ", v.BankLines)).FontSize(7.5f);

                    if (v.Terms != null)
                        content.Item().Text(v.Terms).FontSize(6.5f).FontColor(Muted);
                });

                page.Footer().Row(r =>
                {
                    r.RelativeItem().Text(v.Footer ?? "Thank you.").FontSize(7.5f).FontColor(Muted);
                    r.ConstantItem(Mm(20)).AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7).FontColor(Muted));
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span("/");
                        t.TotalPages();
                    });
                });
            });
        });
    }

    // ------------------------------------------------------------------
    // Thermal receipt (58 / 80 mm roll, continuous length)
    // ------------------------------------------------------------------

    private static IDocument Thermal(InvoiceView v, int widthMm)
    {
        var sale = v.Sale;
        var company = v.Company;
        var narrow = widthMm == 58;
        var size = narrow ? 7f : 8f;
        var margin = narrow ? 2f : 3f;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.ContinuousSize(Mm(widthMm));
                page.Margin(Mm(margin));
                page.DefaultTextStyle(x => x.FontSize(size).FontColor("#000000"));

                page.Content().Column(c =>
                {
                    c.Spacing(1.5f);

                    if (v.Logo != null)
                        c.Item().AlignCenter().Height(Mm(narrow ? 10 : 14)).Image(v.Logo).FitArea();

                    c.Item().AlignCenter().Text(company.Name).Bold().FontSize(size + 2.5f);

                    foreach (var line in AddressLines(company))
                        c.Item().AlignCenter().Text(line).FontSize(size - 0.5f);

                    if (!string.IsNullOrWhiteSpace(company.Phone))
                        c.Item().AlignCenter().Text($"Ph: {company.Phone}").FontSize(size - 0.5f);

                    if (!string.IsNullOrWhiteSpace(company.Gstin))
                        c.Item().AlignCenter().Text($"GSTIN: {company.Gstin}").FontSize(size - 0.5f);

                    Dash(c);

                    c.Item().AlignCenter().Text(v.Title).Bold();
                    Pair(c, "Inv No", sale.InvoiceNumber, size, bold: true);
                    Pair(c, "Date", $"{sale.SaleDate:dd-MMM-yyyy}", size);
                    Pair(c, "Customer", sale.Customer.Name, size);

                    if (!string.IsNullOrWhiteSpace(sale.Customer.Gstin))
                        Pair(c, "GSTIN", sale.Customer.Gstin!, size);

                    Dash(c);

                    foreach (var item in sale.Items)
                    {
                        c.Item().Text(t =>
                        {
                            t.DefaultTextStyle(s => s.WrapAnywhere());
                            t.Span(item.ProductName).SemiBold();
                        });

                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text(
                                $"{item.Quantity} x {Money(item.UnitPrice)}" +
                                (v.ShowTax && item.TaxRate is > 0 ? $"  GST {item.TaxRate:0.##}%" : ""))
                                .FontSize(size - 0.5f);
                            r.AutoItem().AlignRight().Text(Money(item.LineTotal ?? item.Total)).SemiBold();
                        });
                    }

                    Dash(c);

                    Pair(c, v.ShowTax ? "Taxable" : "Subtotal", Money(sale.Subtotal), size);

                    if (v.ShowTax)
                    {
                        if (v.Intra)
                        {
                            Pair(c, "CGST", Money(sale.CgstTotal), size);
                            Pair(c, "SGST", Money(sale.SgstTotal), size);
                        }
                        else
                        {
                            Pair(c, "IGST", Money(sale.IgstTotal), size);
                        }
                    }

                    if (sale.RoundOff != 0)
                        Pair(c, "Round off", Money(sale.RoundOff), size);

                    Dash(c);
                    Pair(c, "TOTAL", Money(sale.TotalAmount), size + 3, bold: true);
                    Dash(c);

                    if (v.Tracked)
                    {
                        foreach (var g in v.Payments.GroupBy(p => p.Method))
                            Pair(c, MethodLabel(g.Key), Money(g.Sum(p => p.Amount)), size);

                        Pair(c, "Paid", Money(sale.PaidAmount - sale.RefundedAmount), size);
                        Pair(c, "Balance", Money(sale.Outstanding), size, bold: true);
                        c.Item().AlignCenter().Text(v.Status.Label).Bold().FontColor("#000000");
                    }
                    else
                    {
                        c.Item().AlignCenter().Text("Payment not recorded");
                    }

                    if (v.Terms != null && !narrow)
                    {
                        Dash(c);
                        c.Item().Text(v.Terms).FontSize(size - 1.5f);
                    }

                    Dash(c);
                    c.Item().AlignCenter().Text(v.Footer ?? "Thank you. Visit again.").FontSize(size - 0.5f);
                });
            });
        });
    }

    private static void Dash(ColumnDescriptor c) =>
        c.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor("#000000");

    private static void Pair(ColumnDescriptor c, string label, string value, float size, bool bold = false) =>
        c.Item().Row(r =>
        {
            var l = r.RelativeItem().Text(label).FontSize(size);
            var val = r.AutoItem().PaddingLeft(3).AlignRight().Text(value).FontSize(size);
            if (bold)
            {
                l.Bold();
                val.Bold();
            }
        });
}
using Billing.Application.DTOs.Sales;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Billing.API.Services;

/// <summary>Renders a sale as a printable A4 invoice PDF.</summary>
public class InvoicePdfService
{
    private const string Dark = "#1f2937";
    private const string Muted = "#6b7280";
    private const string Line = "#d1d5db";
    private const string HeaderFill = "#f3f4f6";

    public byte[] Generate(SaleDetailDto sale, byte[]? logo)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var company = sale.Company;
        var showTax = sale.TaxCalculated;

        // "Tax invoice" is only used when the business has a GSTIN and tax was calculated.
        var title = showTax && !string.IsNullOrWhiteSpace(company.Gstin) ? "TAX INVOICE" : "INVOICE";

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Dark));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        if (logo is { Length: > 0 })
                        {
                            row.ConstantItem(70).Height(60).Image(logo).FitArea();
                            row.ConstantItem(10);
                        }

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(company.Name).FontSize(16).Bold();
                            var address = string.Join(", ", new[]
                            {
                                company.Address, company.City, company.State, company.Pincode
                            }.Where(s => !string.IsNullOrWhiteSpace(s)));
                            if (address.Length > 0)
                                c.Item().Text(address).FontColor(Muted);

                            var contact = string.Join("  |  ", new[]
                            {
                                Prefixed("Ph", company.Phone), Prefixed("Email", company.Email)
                            }.Where(s => s != null));
                            if (contact.Length > 0)
                                c.Item().Text(contact).FontColor(Muted);

                            var ids = string.Join("  |  ", new[]
                            {
                                Prefixed("GSTIN", company.Gstin), Prefixed("PAN", company.Pan)
                            }.Where(s => s != null));
                            if (ids.Length > 0)
                                c.Item().Text(ids).SemiBold();
                        });

                        row.ConstantItem(150).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text(title).FontSize(15).Bold();
                            c.Item().AlignRight().Text($"No: {sale.InvoiceNumber}").SemiBold();
                            c.Item().AlignRight().Text($"Date: {sale.SaleDate:dd-MMM-yyyy}");
                        });
                    });

                    header.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Line);
                });

                page.Content().Column(content =>
                {
                    content.Spacing(8);

                    // ---- Bill to / supply details ----
                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Bill To").FontColor(Muted).FontSize(8);
                            c.Item().Text(sale.Customer.Name).Bold();
                            if (!string.IsNullOrWhiteSpace(sale.Customer.Address))
                                c.Item().Text(sale.Customer.Address);
                            c.Item().Text($"Mobile: {sale.Customer.Mobile}");
                            if (!string.IsNullOrWhiteSpace(sale.Customer.Gstin))
                                c.Item().Text($"GSTIN: {sale.Customer.Gstin}");
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            if (showTax && sale.PlaceOfSupplyStateCode != null)
                            {
                                c.Item().AlignRight().Text("Place of Supply").FontColor(Muted).FontSize(8);
                                c.Item().AlignRight().Text(
                                    $"{sale.PlaceOfSupplyState} ({sale.PlaceOfSupplyStateCode})").SemiBold();
                                c.Item().AlignRight().Text(
                                    sale.IsInterState ? "Inter-state supply (IGST)" : "Intra-state supply (CGST + SGST)")
                                    .FontColor(Muted);
                            }
                        });
                    });

                    // ---- Items ----
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(22);
                            columns.RelativeColumn(4);
                            if (showTax) columns.ConstantColumn(48);
                            columns.ConstantColumn(30);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(60);
                            if (showTax)
                            {
                                columns.ConstantColumn(32);
                                columns.ConstantColumn(55);
                            }
                            columns.ConstantColumn(62);
                        });

                        table.Header(header =>
                        {
                            HeaderCell(header.Cell(), "#");
                            HeaderCell(header.Cell(), "Description");
                            if (showTax) HeaderCell(header.Cell(), "HSN");
                            HeaderCell(header.Cell(), "Qty", right: true);
                            HeaderCell(header.Cell(), "Rate", right: true);
                            HeaderCell(header.Cell(), showTax ? "Taxable" : "Amount", right: true);
                            if (showTax)
                            {
                                HeaderCell(header.Cell(), "GST%", right: true);
                                HeaderCell(header.Cell(), "Tax", right: true);
                            }
                            HeaderCell(header.Cell(), "Total", right: true);
                        });

                        var n = 1;
                        foreach (var item in sale.Items)
                        {
                            BodyCell(table.Cell(), n++.ToString());
                            BodyCell(table.Cell(), item.ProductName);
                            if (showTax) BodyCell(table.Cell(), item.HsnCode ?? "");
                            BodyCell(table.Cell(), item.Quantity.ToString(), right: true);
                            BodyCell(table.Cell(), Money(item.UnitPrice), right: true);
                            BodyCell(table.Cell(), Money(item.Total), right: true);
                            if (showTax)
                            {
                                BodyCell(table.Cell(), $"{item.TaxRate:0.##}", right: true);
                                BodyCell(table.Cell(), Money(item.TaxAmount ?? 0), right: true);
                            }
                            BodyCell(table.Cell(), Money(item.LineTotal ?? item.Total), right: true);
                        }
                    });

                    // ---- Totals ----
                    content.Item().AlignRight().Width(230).Column(c =>
                    {
                        TotalRow(c, showTax ? "Taxable value" : "Subtotal", sale.Subtotal);

                        if (showTax)
                        {
                            if (sale.CgstTotal != 0 || sale.SgstTotal != 0)
                            {
                                TotalRow(c, "CGST", sale.CgstTotal);
                                TotalRow(c, "SGST", sale.SgstTotal);
                            }
                            if (sale.IgstTotal != 0)
                                TotalRow(c, "IGST", sale.IgstTotal);
                            if (sale.TaxTotal == 0)
                                TotalRow(c, "Tax", 0);
                            if (sale.RoundOff != 0)
                                TotalRow(c, "Round off", sale.RoundOff);
                        }

                        c.Item().PaddingTop(2).LineHorizontal(1).LineColor(Dark);
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Grand Total").Bold().FontSize(11);
                            r.ConstantItem(90).AlignRight().Text($"Rs. {Money(sale.TotalAmount)}").Bold().FontSize(11);
                        });
                    });

                    content.Item().Text(sale.AmountInWords).Italic().FontColor(Muted);

                    if (!showTax)
                    {
                        content.Item().Text(
                            "Tax was not recorded for this invoice (created before GST details were captured).")
                            .FontSize(8).FontColor(Muted);
                    }

                    // ---- Payment status ----
                    content.Item().PaddingTop(4).Column(c =>
                    {
                        if (sale.PaymentTracked)
                        {
                            var line = $"Paid: Rs. {Money(sale.PaidAmount)}   |   Balance due: Rs. {Money(sale.Outstanding)}";
                            c.Item().Text(line).SemiBold();
                        }
                        else
                        {
                            c.Item().Text("Payment status: not recorded").FontColor(Muted);
                        }
                    });

                    // ---- Bank / UPI ----
                    var bank = new[]
                    {
                        Prefixed("Bank", company.BankName), Prefixed("A/c", company.AccountNumber),
                        Prefixed("IFSC", company.IfscCode), Prefixed("UPI", company.UpiId)
                    }.Where(s => s != null).ToList();

                    if (bank.Count > 0)
                    {
                        content.Item().Column(c =>
                        {
                            c.Item().Text("Payment details").FontColor(Muted).FontSize(8);
                            c.Item().Text(string.Join("   |   ", bank));
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(company.Terms))
                    {
                        content.Item().Column(c =>
                        {
                            c.Item().Text("Terms & conditions").FontColor(Muted).FontSize(8);
                            c.Item().Text(company.Terms).FontSize(8);
                        });
                    }
                });

                page.Footer().Column(f =>
                {
                    f.Item().LineHorizontal(1).LineColor(Line);
                    f.Item().PaddingTop(3).Row(r =>
                    {
                        r.RelativeItem().Text(company.Footer ?? "Thank you for your business.")
                            .FontColor(Muted).FontSize(8);
                        r.ConstantItem(120).AlignRight().Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(8).FontColor(Muted));
                            t.Span("Page ");
                            t.CurrentPageNumber();
                            t.Span(" of ");
                            t.TotalPages();
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    private static string Money(decimal value) => value.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("en-IN"));

    private static string? Prefixed(string label, string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : $"{label}: {value}";

    private static void HeaderCell(IContainer cell, string text, bool right = false)
    {
        var c = cell.Background(HeaderFill).BorderBottom(1).BorderColor(Line).Padding(3);
        (right ? c.AlignRight() : c).Text(text).Bold().FontSize(8);
    }

    private static void BodyCell(IContainer cell, string text, bool right = false)
    {
        var c = cell.BorderBottom(0.5f).BorderColor(Line).Padding(3);
        (right ? c.AlignRight() : c).Text(text);
    }

    private static void TotalRow(ColumnDescriptor c, string label, decimal amount)
    {
        c.Item().Row(r =>
        {
            r.RelativeItem().Text(label).FontColor(Muted);
            r.ConstantItem(90).AlignRight().Text($"Rs. {Money(amount)}");
        });
    }
}

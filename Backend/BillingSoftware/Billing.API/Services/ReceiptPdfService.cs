using Billing.Application.DTOs.Sales;
using Billing.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static Billing.API.Services.PdfParts;

namespace Billing.API.Services;

/// <summary>
/// Renders a payment receipt: proof that money was received against an invoice. It is
/// not a tax invoice. It states this payment, what was paid before and what is still due,
/// so a part payment can never read as "invoice paid".
/// </summary>
public class ReceiptPdfService
{
    public byte[] Generate(PaymentReceiptDto receipt, InvoiceFormat format, int thermalWidthMm, byte[]? logo) =>
        Compose(receipt, format, thermalWidthMm, logo).GeneratePdf();

    public IDocument Compose(PaymentReceiptDto receipt, InvoiceFormat format, int thermalWidthMm, byte[]? logo)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var showLogo = receipt.Settings.ShowLogo && logo is { Length: > 0 } ? logo : null;

        return format == InvoiceFormat.Thermal
            ? Thermal(receipt, thermalWidthMm == 58 ? 58 : 80, showLogo)
            : Standard(receipt, showLogo);
    }

    private static string Headline(PaymentReceiptDto r) =>
        r.IsVoided ? "VOIDED PAYMENT"
        : r.SettlesInvoice ? "FINAL PAYMENT - INVOICE FULLY PAID"
        : $"PART PAYMENT - BALANCE Rs. {Money(r.BalanceAfter)} STILL DUE";

    private static string HeadlineColour(PaymentReceiptDto r) =>
        r.IsVoided ? "#b91c1c" : r.SettlesInvoice ? "#15803d" : "#b45309";

    // ---------------------------------------------------------------- A5 receipt

    private static IDocument Standard(PaymentReceiptDto r, byte[]? logo)
    {
        var company = r.Company;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(10, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Ink));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        if (logo != null)
                        {
                            row.ConstantItem(Mm(18)).Height(Mm(14)).Image(logo).FitArea();
                            row.ConstantItem(Mm(3));
                        }

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(company.Name).FontSize(13).Bold().FontColor(Accent);

                            foreach (var line in AddressLines(company))
                                c.Item().Text(line).FontSize(8).FontColor(Muted);

                            if (ContactLine(company) is { } contact)
                                c.Item().Text(contact).FontSize(8).FontColor(Muted);

                            if (!string.IsNullOrWhiteSpace(company.Gstin))
                                c.Item().Text($"GSTIN: {company.Gstin}").FontSize(8);
                        });
                    });

                    header.Item().PaddingTop(4).LineHorizontal(1).LineColor(Accent);
                });

                page.Content().PaddingTop(8).Column(c =>
                {
                    c.Spacing(7);

                    c.Item().Row(row =>
                    {
                        row.RelativeItem().Text("PAYMENT RECEIPT").FontSize(14).Bold().FontColor(Accent);
                        row.ConstantItem(Mm(50)).Column(m =>
                        {
                            m.Item().AlignRight().Text($"Receipt No: {r.ReceiptNumber}").Bold();
                            m.Item().AlignRight().Text($"Date: {r.PaymentDate.ToLocalTime():dd-MMM-yyyy HH:mm}");
                        });
                    });

                    c.Item().Border(0.5f).BorderColor(Rule).Padding(6).Column(d =>
                    {
                        void Row(string label, string value) => d.Item().Row(x =>
                        {
                            x.ConstantItem(Mm(32)).Text(label).FontColor(Muted);
                            x.RelativeItem().Text(value);
                        });

                        Row("Received from", r.Customer.Name);
                        Row("Against invoice", $"{r.InvoiceNumber}  ({r.InvoiceDate:dd-MMM-yyyy})");
                        Row("Payment method", MethodLabel(r.Method));

                        if (!string.IsNullOrWhiteSpace(r.Reference))
                            Row("Reference", r.Reference!);

                        if (!string.IsNullOrWhiteSpace(r.ReceivedBy))
                            Row("Received by", r.ReceivedBy!);
                    });

                    c.Item().Border(0.5f).BorderColor(Rule).Padding(6).Column(a =>
                    {
                        void Line(string label, string value, bool bold = false, float size = 9)
                        {
                            a.Item().PaddingVertical(1.5f).Row(x =>
                            {
                                var l = x.RelativeItem().Text(label).FontSize(size);
                                var v = x.ConstantItem(Mm(38)).AlignRight().Text(value).FontSize(size);

                                if (bold)
                                {
                                    l.Bold();
                                    v.Bold();
                                }
                                else
                                {
                                    l.FontColor(Muted);
                                }
                            });
                        }

                        Line("Invoice total", Rs(r.InvoiceTotal));
                        Line("Paid earlier", Rs(r.PreviouslyPaid));
                        a.Item().LineHorizontal(0.5f).LineColor(Rule);

                        if (r.IsVoided)
                        {
                            Line("Amount (voided, not counted)", Rs(r.Amount), size: 10);
                        }
                        else
                        {
                            Line("AMOUNT RECEIVED", Rs(r.Amount), bold: true, size: 12);
                        }

                        a.Item().LineHorizontal(0.5f).LineColor(Rule);
                        Line("Balance after this payment", Rs(r.BalanceAfter), bold: true);
                    });

                    if (!r.IsVoided)
                        c.Item().Text(r.AmountInWords).Italic().FontColor(Muted).FontSize(8);

                    c.Item().Border(1).BorderColor(HeadlineColour(r)).Padding(5).AlignCenter()
                        .Text(Headline(r)).Bold().FontColor(HeadlineColour(r));

                    if (r.IsVoided)
                        c.Item().Text(
                            $"This payment was voided{(r.VoidedOn.HasValue ? $" on {r.VoidedOn.Value.ToLocalTime():dd-MMM-yyyy}" : "")}" +
                            $"{(string.IsNullOrWhiteSpace(r.VoidReason) ? "." : $": {r.VoidReason}")} It is not counted towards the invoice.")
                            .FontColor("#b91c1c").FontSize(8);

                    if (!string.IsNullOrWhiteSpace(r.Notes))
                        c.Item().Text($"Note: {r.Notes}").FontSize(8).FontColor(Muted);

                    c.Item().AlignRight().Width(Mm(50)).PaddingTop(14).Column(s =>
                    {
                        s.Item().LineHorizontal(0.5f).LineColor(Ink);
                        s.Item().AlignCenter().Text($"For {company.Name}").FontSize(8);
                    });
                });

                page.Footer().Column(f =>
                {
                    if (r.Settings.ShowFooter && !string.IsNullOrWhiteSpace(company.Footer))
                        f.Item().AlignCenter().Text(company.Footer).FontSize(8).FontColor(Muted);

                    f.Item().AlignCenter().Text("This is a payment receipt, not a tax invoice.")
                        .FontSize(7).FontColor(Muted);
                });
            });
        });
    }

    // ---------------------------------------------------------------- thermal receipt

    private static IDocument Thermal(PaymentReceiptDto r, int widthMm, byte[]? logo)
    {
        var narrow = widthMm == 58;
        var size = narrow ? 7f : 8f;
        var company = r.Company;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.ContinuousSize(Mm(widthMm));
                page.Margin(Mm(narrow ? 2 : 3));
                page.DefaultTextStyle(x => x.FontSize(size).FontColor("#000000"));

                page.Content().Column(c =>
                {
                    c.Spacing(1.5f);

                    if (logo != null)
                        c.Item().AlignCenter().Height(Mm(narrow ? 10 : 14)).Image(logo).FitArea();

                    c.Item().AlignCenter().Text(company.Name).Bold().FontSize(size + 2.5f);

                    if (!string.IsNullOrWhiteSpace(company.Phone))
                        c.Item().AlignCenter().Text($"Ph: {company.Phone}").FontSize(size - 0.5f);

                    Dash(c);
                    c.Item().AlignCenter().Text("PAYMENT RECEIPT").Bold();
                    Pair(c, "Receipt", r.ReceiptNumber, size, true);
                    Pair(c, "Date", $"{r.PaymentDate.ToLocalTime():dd-MMM-yy HH:mm}", size);
                    Pair(c, "Invoice", r.InvoiceNumber, size);
                    Pair(c, "Customer", r.Customer.Name, size);
                    Pair(c, "Method", MethodLabel(r.Method), size);

                    if (!string.IsNullOrWhiteSpace(r.Reference))
                        Pair(c, "Ref", r.Reference!, size);

                    Dash(c);
                    Pair(c, "Invoice total", Money(r.InvoiceTotal), size);
                    Pair(c, "Paid earlier", Money(r.PreviouslyPaid), size);
                    Pair(c, r.IsVoided ? "VOIDED" : "RECEIVED", Money(r.Amount), size + 2, true);
                    Pair(c, "Balance", Money(r.BalanceAfter), size, true);
                    Dash(c);

                    c.Item().AlignCenter().Text(
                        r.IsVoided ? "VOIDED PAYMENT"
                        : r.SettlesInvoice ? "FINAL PAYMENT - PAID IN FULL"
                        : "PART PAYMENT - BALANCE DUE").Bold();

                    Dash(c);
                    c.Item().AlignCenter().Text(
                        r.Settings.ShowFooter && !string.IsNullOrWhiteSpace(company.Footer)
                            ? company.Footer!
                            : "Thank you.").FontSize(size - 0.5f);
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
            var v = r.AutoItem().PaddingLeft(3).AlignRight().Text(value).FontSize(size);

            if (bold)
            {
                l.Bold();
                v.Bold();
            }
        });
}
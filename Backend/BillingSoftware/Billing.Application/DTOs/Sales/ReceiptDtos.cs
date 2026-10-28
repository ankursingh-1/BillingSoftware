namespace Billing.Application.DTOs.Sales
{
    /// <summary>Print preferences from company settings.</summary>
    public class InvoiceSettingsDto
    {
        /// <summary>A4, Compact or Thermal.</summary>
        public string DefaultFormat { get; set; } = "A4";

        /// <summary>58 or 80.</summary>
        public int ThermalPaperWidthMm { get; set; } = 80;
        public bool ShowBankDetails { get; set; }
        public bool ShowLogo { get; set; } = true;
        public bool ShowTerms { get; set; } = true;
        public bool ShowFooter { get; set; } = true;
    }

    /// <summary>Proof of one payment received against an invoice.</summary>
    public class PaymentReceiptDto
    {
        public int PaymentId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public string? ReceivedBy { get; set; }

        /// <summary>Received or Voided. A voided payment is not counted anywhere.</summary>
        public string Status { get; set; } = "Received";
        public bool IsVoided { get; set; }
        public DateTime? VoidedOn { get; set; }
        public string? VoidReason { get; set; }

        public int SaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }

        /// <summary>Invoice total less any returns made before this payment.</summary>
        public decimal InvoiceTotal { get; set; }

        /// <summary>Counted payments made before this one (voided payments excluded).</summary>
        public decimal PreviouslyPaid { get; set; }

        /// <summary>Still to be paid after this payment (what it was when the payment was received).</summary>
        public decimal BalanceAfter { get; set; }

        /// <summary>True only when this payment cleared the invoice.</summary>
        public bool SettlesInvoice { get; set; }

        /// <summary>"Part payment" or "Final payment" (or "Voided").</summary>
        public string Description { get; set; } = string.Empty;

        public string AmountInWords { get; set; } = string.Empty;

        public SaleCustomerDto Customer { get; set; } = new();
        public InvoiceCompanyDto Company { get; set; } = new();
        public InvoiceSettingsDto Settings { get; set; } = new();
    }
}
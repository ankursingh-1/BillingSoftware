using System.ComponentModel.DataAnnotations;
using Billing.Domain.Enums;

namespace Billing.Application.DTOs.Sales
{
    public class RecordPaymentRequest
    {
        [EnumDataType(typeof(PaymentMethod), ErrorMessage = "Select a valid payment method.")]
        public PaymentMethod Method { get; set; }

        [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Payment amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [StringLength(100, ErrorMessage = "Reference cannot exceed 100 characters.")]
        public string? Reference { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        /// <summary>
        /// Required to record a payment on an invoice created before payments were tracked.
        /// Confirms the user understands payment tracking starts now, so the balance becomes
        /// "invoice total minus the payments recorded in this system".
        /// </summary>
        public bool StartTracking { get; set; }
    }

    public class VoidPaymentRequest
    {
        [Required(ErrorMessage = "A reason is required to void a payment.")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}

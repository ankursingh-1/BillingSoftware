using System.ComponentModel.DataAnnotations;
using Billing.Domain.Enums;

namespace Billing.Application.DTOs.Sales
{
    public class CreateSalePaymentRequest
    {
        [EnumDataType(typeof(PaymentMethod), ErrorMessage = "Select a valid payment method.")]
        public PaymentMethod Method { get; set; }

        [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Payment amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [StringLength(100, ErrorMessage = "Reference cannot exceed 100 characters.")]
        public string? Reference { get; set; }
    }
}

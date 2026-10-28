namespace Billing.Domain.Enums;

/// <summary>How a payment (or refund) was made. Stored as its integer value.</summary>
public enum PaymentMethod
{
    Cash = 1,
    Upi = 2,
    Card = 3,
    BankTransfer = 4,
    Cheque = 5,
    Other = 6
}

namespace Billing.Application.Accounting;

public enum PaymentStatus
{
    /// <summary>Legacy sale: payments were never recorded, so the status is unknown.</summary>
    NotRecorded,
    Unpaid,
    Partial,
    Paid,
    /// <summary>Nothing left to pay because the goods were returned.</summary>
    Settled
}

/// <param name="NetPayable">Invoice total minus the credit value of returns (never below 0).</param>
/// <param name="NetPaid">Payments received minus refunds handed back.</param>
/// <param name="Outstanding">What the customer still owes.</param>
/// <param name="RefundDue">What the shop still owes the customer (paid more than the net payable).</param>
public readonly record struct PaymentSummary(
    decimal NetPayable,
    decimal NetPaid,
    decimal Outstanding,
    decimal RefundDue,
    PaymentStatus Status);

public static class PaymentCalculator
{
    public static PaymentSummary Summarise(
        decimal invoiceTotal,
        decimal returnsTotal,
        decimal paid,
        decimal refunded,
        bool paymentTracked)
    {
        var netPayable = Math.Max(0, invoiceTotal - returnsTotal);
        var netPaid = paid - refunded;
        var outstanding = Math.Max(0, netPayable - netPaid);
        var refundDue = Math.Max(0, netPaid - netPayable);

        PaymentStatus status;
        if (!paymentTracked)
            status = PaymentStatus.NotRecorded;
        else if (netPayable == 0 && outstanding == 0)
            status = PaymentStatus.Settled;
        else if (outstanding == 0)
            status = PaymentStatus.Paid;
        else if (netPaid <= 0)
            status = PaymentStatus.Unpaid;
        else
            status = PaymentStatus.Partial;

        return new PaymentSummary(netPayable, netPaid, outstanding, refundDue, status);
    }
}

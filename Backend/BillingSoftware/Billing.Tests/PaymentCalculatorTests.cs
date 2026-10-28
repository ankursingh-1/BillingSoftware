using Billing.Application.Accounting;

namespace Billing.Tests;

public class PaymentCalculatorTests
{
    [Fact]
    public void Legacy_sale_is_not_recorded_whatever_the_numbers()
    {
        var s = PaymentCalculator.Summarise(1000, 0, 0, 0, paymentTracked: false);
        Assert.Equal(PaymentStatus.NotRecorded, s.Status);
    }

    [Fact]
    public void Nothing_paid_is_unpaid()
    {
        var s = PaymentCalculator.Summarise(1000, 0, 0, 0, true);
        Assert.Equal(PaymentStatus.Unpaid, s.Status);
        Assert.Equal(1000, s.Outstanding);
    }

    [Fact]
    public void Part_payment_is_partial_with_balance()
    {
        var s = PaymentCalculator.Summarise(1000, 0, 400, 0, true);
        Assert.Equal(PaymentStatus.Partial, s.Status);
        Assert.Equal(600, s.Outstanding);
    }

    [Fact]
    public void Full_payment_is_paid()
    {
        var s = PaymentCalculator.Summarise(1000, 0, 1000, 0, true);
        Assert.Equal(PaymentStatus.Paid, s.Status);
        Assert.Equal(0, s.Outstanding);
    }

    [Fact]
    public void Return_of_unpaid_goods_reduces_what_is_owed()
    {
        var s = PaymentCalculator.Summarise(1000, 300, 0, 0, true);
        Assert.Equal(700, s.NetPayable);
        Assert.Equal(700, s.Outstanding);
        Assert.Equal(PaymentStatus.Unpaid, s.Status);
    }

    [Fact]
    public void Return_after_full_payment_creates_a_refund_due_until_it_is_refunded()
    {
        var before = PaymentCalculator.Summarise(1000, 300, 1000, 0, true);
        Assert.Equal(300, before.RefundDue);
        Assert.Equal(0, before.Outstanding);

        var after = PaymentCalculator.Summarise(1000, 300, 1000, 300, true);
        Assert.Equal(0, after.RefundDue);
        Assert.Equal(PaymentStatus.Paid, after.Status);
    }

    [Fact]
    public void Fully_returned_unpaid_sale_is_settled_not_paid()
    {
        var s = PaymentCalculator.Summarise(1000, 1000, 0, 0, true);
        Assert.Equal(PaymentStatus.Settled, s.Status);
        Assert.Equal(0, s.Outstanding);
    }
}

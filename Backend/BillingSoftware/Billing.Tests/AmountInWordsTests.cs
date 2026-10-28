using Billing.Application.Accounting;

namespace Billing.Tests;

public class AmountInWordsTests
{
    [Theory]
    [InlineData(0, "Rupees Zero Only")]
    [InlineData(1, "Rupees One Only")]
    [InlineData(19, "Rupees Nineteen Only")]
    [InlineData(21, "Rupees Twenty One Only")]
    [InlineData(100, "Rupees One Hundred Only")]
    [InlineData(1234, "Rupees One Thousand Two Hundred Thirty Four Only")]
    [InlineData(100000, "Rupees One Lakh Only")]
    [InlineData(1234567, "Rupees Twelve Lakh Thirty Four Thousand Five Hundred Sixty Seven Only")]
    [InlineData(10000000, "Rupees One Crore Only")]
    [InlineData(169.5, "Rupees One Hundred Sixty Nine and Fifty Paise Only")]
    [InlineData(0.05, "Rupees Zero and Five Paise Only")]
    public void Converts_in_the_indian_system(double amount, string expected) =>
        Assert.Equal(expected, AmountInWords.Rupees((decimal)amount));
}

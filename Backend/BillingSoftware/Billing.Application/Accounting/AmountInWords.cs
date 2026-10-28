namespace Billing.Application.Accounting;

/// <summary>Indian-system amount in words, e.g. "Rupees One Lakh Twenty Thousand Only".</summary>
public static class AmountInWords
{
    private static readonly string[] Ones =
    {
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven",
        "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
    };

    private static readonly string[] Tens =
    {
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    };

    public static string Rupees(decimal amount)
    {
        amount = Math.Round(Math.Abs(amount), 2, MidpointRounding.AwayFromZero);
        var rupees = (long)Math.Truncate(amount);
        var paise = (int)Math.Round((amount - rupees) * 100m);

        var words = "Rupees " + (rupees == 0 ? "Zero" : Convert(rupees));
        if (paise > 0)
            words += " and " + Convert(paise) + " Paise";

        return words + " Only";
    }

    private static string Convert(long n)
    {
        var parts = new List<string>();

        void Take(long divisor, string name)
        {
            if (n >= divisor)
            {
                parts.Add(Below100000(n / divisor) + " " + name);
                n %= divisor;
            }
        }

        Take(10_000_000, "Crore");
        Take(100_000, "Lakh");
        Take(1_000, "Thousand");

        if (n >= 100)
        {
            parts.Add(Ones[n / 100] + " Hundred");
            n %= 100;
        }

        if (n > 0)
            parts.Add(Below100(n));

        return string.Join(" ", parts);
    }

    private static string Below100000(long n) => n >= 100 ? Convert(n) : Below100(n);

    private static string Below100(long n) =>
        n < 20 ? Ones[n] : (Tens[n / 10] + (n % 10 > 0 ? " " + Ones[n % 10] : "")).Trim();
}

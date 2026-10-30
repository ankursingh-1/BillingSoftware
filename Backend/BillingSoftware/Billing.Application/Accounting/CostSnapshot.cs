namespace Billing.Application.Accounting;

/// <summary>
/// Catalogue cost at a point in time. The stock ledger stores quantities only,
/// so this application costs sold goods at the last recorded purchase/opening
/// cost on the product (<c>Product.PurchasePrice</c>), snapshotted onto the
/// sale line. Zero means no cost has been recorded — never treat it as free.
/// </summary>
public static class CostSnapshot
{
    public static decimal? FromCatalogue(decimal purchasePrice) =>
        purchasePrice > 0 ? GstCalculator.Round(purchasePrice) : null;
}

/// <summary>
/// Reconstructs a historical cost only from an auditable purchase that existed
/// on or before the sale. Never uses today's catalogue price.
/// </summary>
public static class CostBackfill
{
    public static decimal? Propose(
        decimal? existingCost,
        DateTime saleDate,
        IEnumerable<(DateTime PurchaseDate, decimal Price)> purchases)
    {
        if (existingCost.HasValue)
            return existingCost;

        var last = purchases
            .Where(p => p.PurchaseDate <= saleDate && p.Price > 0)
            .OrderByDescending(p => p.PurchaseDate)
            .Select(p => p.Price)
            .FirstOrDefault();

        return last > 0 ? GstCalculator.Round(last) : null;
    }

    /// <summary>
    /// Cost used for gross profit: a stored sale-line snapshot when it is
    /// greater than zero, otherwise the last purchase on or before the sale.
    /// Zero and null snapshots are unknown (never free). Does not write.
    /// </summary>
    public static decimal? ForProfit(
        decimal? saleLineSnapshot,
        DateTime saleDate,
        IEnumerable<(DateTime PurchaseDate, decimal Price)> purchases)
    {
        var stored = saleLineSnapshot is > 0 ? saleLineSnapshot : null;
        return Propose(stored, saleDate, purchases);
    }
}

namespace Billing.Tests;

public class CostSnapshotCaptureTests
{
    [Fact]
    public void New_sale_snapshots_cost_before_stock_is_reduced()
    {
        var src = File.ReadAllText(Find("SaleService.cs"));
        var cost = src.IndexOf("var costs = await ResolveCostsAsync", StringComparison.Ordinal);
        var assign = src.IndexOf("CostPrice = costs[product.Id]", StringComparison.Ordinal);
        var stock = src.IndexOf("StockTransactionType.Sale", StringComparison.Ordinal);

        Assert.True(cost >= 0, "ResolveCostsAsync must run when creating a sale.");
        Assert.True(assign > cost, "CostPrice must be taken from the resolved snapshot.");
        Assert.True(stock > assign, "Stock must be reduced after the cost snapshot is stored.");
        Assert.Contains("CostSnapshot.FromCatalogue", src);
    }

    [Fact]
    public void Gross_profit_uses_snapshot_or_last_purchase_before_sale_and_never_writes()
    {
        var src = File.ReadAllText(Find("GrossProfitService.cs"));
        Assert.Contains("CostBackfill.ForProfit", src);
        Assert.Contains("PurchaseItems", src);
        Assert.Contains("PurchaseDate", src);
        Assert.Contains("line.CostPrice", src);
        Assert.DoesNotContain("SaveChanges", src);
        Assert.DoesNotContain("Product.PurchasePrice", src);
        Assert.DoesNotContain("TotalPurchase", src);
    }

    [Fact]
    public void Warranty_replacement_is_stock_only_and_cannot_enter_gross_profit()
    {
        var warranty = File.ReadAllText(Find("WarrantyService.cs"));
        Assert.Contains("StockTransactionType.WarrantyReplacement", warranty);
        Assert.DoesNotContain("new Sale", warranty);
        Assert.DoesNotContain("SaleItems.Add", warranty);
    }

    private static string Find(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var matches = dir.GetFiles(fileName, SearchOption.AllDirectories);
            if (matches.Length > 0)
                return matches[0].FullName;
            dir = dir.Parent;
        }

        throw new FileNotFoundException(fileName);
    }
}

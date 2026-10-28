namespace Billing.Application.Inventory;

public enum StockStatus
{
    InStock = 0,
    Low = 1,
    Out = 2
}

/// <summary>
/// One definition of "low" and "out of stock" for the dashboard, reports and the
/// inventory pages, so the numbers always agree. Out = no stock; Low = 1 to the
/// threshold (configurable as Inventory:LowStockThreshold, default 5).
/// </summary>
public sealed class StockAlertPolicy
{
    public const int DefaultLowStockThreshold = 5;
    public const int MaxLowStockThreshold = 100_000;

    public StockAlertPolicy(int lowStockThreshold)
    {
        LowStockThreshold = Math.Clamp(lowStockThreshold, 0, MaxLowStockThreshold);
    }

    public int LowStockThreshold { get; }

    public StockStatus Classify(int stock) =>
        stock <= 0 ? StockStatus.Out
        : stock <= LowStockThreshold ? StockStatus.Low
        : StockStatus.InStock;
}
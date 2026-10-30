-- Preview only. Do not UPDATE without explicit approval.
-- Reconstructs a proposed CostPrice from the last purchase line on or before
-- the sale. Never overwrites an existing snapshot. Never uses today's
-- Products.PurchasePrice. Opening-stock ledger rows have no unit cost, so
-- they cannot reconstruct a missing snapshot.

SET NOCOUNT ON;

SELECT
    si.Id AS SaleItemId,
    s.Id AS SaleId,
    s.InvoiceNumber,
    s.SaleDate,
    p.SKU,
    p.Name AS ProductName,
    si.Quantity,
    si.SellingPrice,
    si.CostPrice AS ExistingCostPrice,
    lastBuy.PurchaseDate AS LastPurchaseOnOrBeforeSale,
    lastBuy.PurchasePrice AS ProposedCostPrice,
    p.PurchasePrice AS CurrentCataloguePrice
FROM SaleItems si
INNER JOIN Sales s ON s.Id = si.SaleId
INNER JOIN Products p ON p.Id = si.ProductId
OUTER APPLY (
    SELECT TOP (1)
        pu.PurchaseDate,
        pi.PurchasePrice
    FROM PurchaseItems pi
    INNER JOIN Purchases pu ON pu.Id = pi.PurchaseId
    WHERE pi.ProductId = si.ProductId
      AND pi.PurchasePrice > 0
      AND pu.PurchaseDate <= s.SaleDate
    ORDER BY pu.PurchaseDate DESC, pi.Id DESC
) lastBuy
WHERE si.CostPrice IS NULL
ORDER BY s.SaleDate, s.Id, si.Id;

SELECT
    COUNT(*) AS SaleLines,
    SUM(CASE WHEN si.CostPrice IS NOT NULL THEN 1 ELSE 0 END) AS KnownCost,
    SUM(CASE WHEN si.CostPrice IS NULL THEN 1 ELSE 0 END) AS UnknownCost,
    SUM(CASE WHEN si.CostPrice IS NULL AND lastBuy.PurchasePrice IS NOT NULL THEN 1 ELSE 0 END) AS ReconstructableIfApproved
FROM SaleItems si
INNER JOIN Sales s ON s.Id = si.SaleId
OUTER APPLY (
    SELECT TOP (1) pi.PurchasePrice
    FROM PurchaseItems pi
    INNER JOIN Purchases pu ON pu.Id = pi.PurchaseId
    WHERE pi.ProductId = si.ProductId
      AND pi.PurchasePrice > 0
      AND pu.PurchaseDate <= s.SaleDate
    ORDER BY pu.PurchaseDate DESC, pi.Id DESC
) lastBuy;

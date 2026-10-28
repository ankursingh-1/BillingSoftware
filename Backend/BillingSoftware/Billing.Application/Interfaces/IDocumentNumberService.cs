namespace Billing.Application.Interfaces;

public interface IDocumentNumberService
{
    /// <summary>
    /// Next number in a series (e.g. "INV:2627"). Must be called inside a database
    /// transaction: the increment is rolled back with the document, so numbers are gapless.
    /// </summary>
    Task<int> NextAsync(string seriesKey);
}

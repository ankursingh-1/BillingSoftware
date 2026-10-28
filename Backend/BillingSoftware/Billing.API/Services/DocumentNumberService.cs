using Billing.Application.Interfaces;
using Billing.Persistence.Context;

namespace Billing.API.Services;

public class DocumentNumberService : IDocumentNumberService
{
    private readonly BillingDbContext _context;

    public DocumentNumberService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<int> NextAsync(string seriesKey)
    {
        // MERGE ... WITH (HOLDLOCK) is an atomic upsert-and-increment. The row lock is
        // held until the surrounding transaction ends, so concurrent documents are
        // serialised for this series and each gets a distinct number.
        const string sql = @"
MERGE DocumentSequences WITH (HOLDLOCK) AS target
USING (SELECT @key AS SeriesKey) AS source
   ON target.SeriesKey = source.SeriesKey
WHEN MATCHED THEN UPDATE SET LastNumber = target.LastNumber + 1
WHEN NOT MATCHED THEN INSERT (SeriesKey, LastNumber) VALUES (@key, 1)
OUTPUT inserted.LastNumber;";

        await using var command = SqlTransactionHelper.CreateCommand(
            _context, sql, ("@key", seriesKey));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}

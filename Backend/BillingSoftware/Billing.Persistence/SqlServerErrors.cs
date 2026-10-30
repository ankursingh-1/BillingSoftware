using Microsoft.Data.SqlClient;

namespace Billing.Persistence;

/// <summary>
/// SQL Server errors that mean this database does not yet have a table or column
/// the current model expects (for example Warranties on an unmigrated copy).
/// </summary>
public static class SqlServerErrors
{
    public const int InvalidObject = 208;
    public const int InvalidColumn = 207;

    public static bool IsMissingObject(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Number is InvalidObject or InvalidColumn)
                return true;
        }

        return false;
    }
}

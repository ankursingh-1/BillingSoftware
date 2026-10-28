using System.Data.Common;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Billing.API.Services;

/// <summary>Small helpers for raw SQL that must run inside the current EF transaction.</summary>
internal static class SqlTransactionHelper
{
    /// <summary>
    /// Creates a command enlisted in the context's current transaction.
    /// Throws if there is none: these statements are only safe inside one.
    /// </summary>
    public static DbCommand CreateCommand(BillingDbContext context, string sql,
        params (string Name, object Value)[] parameters)
    {
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "This operation must run inside a database transaction.");

        var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    /// <summary>
    /// Takes an exclusive row lock on a sale until the transaction ends, so concurrent
    /// payments, voids and returns for the same invoice run one after another.
    /// </summary>
    public static async Task LockSaleAsync(BillingDbContext context, int saleId)
    {
        await using var command = CreateCommand(
            context,
            "UPDATE Sales SET modifieson = @now WHERE Id = @id;",
            ("@now", DateTime.UtcNow), ("@id", saleId));

        if (await command.ExecuteNonQueryAsync() == 0)
            throw Billing.Shared.Exceptions.BusinessException.NotFound("Sale not found.");
    }

    /// <summary>Same as <see cref="LockSaleAsync"/> for a purchase.</summary>
    public static async Task LockPurchaseAsync(BillingDbContext context, int purchaseId)
    {
        await using var command = CreateCommand(
            context,
            "UPDATE Purchases SET modifieson = @now WHERE Id = @id;",
            ("@now", DateTime.UtcNow), ("@id", purchaseId));

        if (await command.ExecuteNonQueryAsync() == 0)
            throw Billing.Shared.Exceptions.BusinessException.NotFound("Purchase not found.");
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        ConduitContext context,
        CancellationToken cancellationToken = default
    )
    {
        var applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        if (!applied.Any() && await context.Database.CanConnectAsync(cancellationToken))
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                await using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = context.Database.IsSqlite()
                    ? "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Persons'"
                    : "SELECT COUNT(*) FROM sys.tables WHERE name = 'Persons' AND schema_id = SCHEMA_ID('dbo')";
                if (
                    Convert.ToInt32(
                        await command.ExecuteScalarAsync(cancellationToken),
                        System.Globalization.CultureInfo.InvariantCulture
                    ) != 0
                )
                {
                    throw new InvalidOperationException(
                        "This database predates EF migrations. Back it up and follow docs/database-migrations.md to baseline the existing schema before starting the API."
                    );
                }
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
        await context.Database.MigrateAsync(cancellationToken);
    }
}

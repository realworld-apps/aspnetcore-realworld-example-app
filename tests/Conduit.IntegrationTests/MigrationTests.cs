using System;
using System.Linq;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Conduit.IntegrationTests;

public class MigrationTests
{
    [Fact]
    public async Task Fresh_Sqlite_Database_Migrates_And_Can_Restart()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new SqliteConduitContext(
            new DbContextOptionsBuilder<SqliteConduitContext>().UseSqlite(connection).Options
        );
        await DatabaseInitializer.InitializeAsync(db);
        await DatabaseInitializer.InitializeAsync(db);
        Assert.Equal(2, (await db.Database.GetAppliedMigrationsAsync()).Count());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Legacy_Schema_Requires_Explicit_Baseline_And_Preserves_Data()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new SqliteConduitContext(
            new DbContextOptionsBuilder<SqliteConduitContext>().UseSqlite(connection).Options
        );
        await db.GetService<IMigrator>().MigrateAsync("InitialSchema");
        db.Add(new Person { Username = "existing", Email = "existing@example.com" });
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE __EFMigrationsHistory");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DatabaseInitializer.InitializeAsync(db)
        );
        Assert.Contains("baseline", exception.Message);

        // The documented opt-in baseline marks only the old schema, not the new indexes.
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TABLE __EFMigrationsHistory (MigrationId TEXT NOT NULL PRIMARY KEY, ProductVersion TEXT NOT NULL)"
        );
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO __EFMigrationsHistory VALUES ('20261006170650_InitialSchema', '10.0.12')"
        );
        await DatabaseInitializer.InitializeAsync(db);
        Assert.Equal("existing", (await db.Persons.SingleAsync()).Username);
        Assert.Equal(2, (await db.Database.GetAppliedMigrationsAsync()).Count());
    }

    [Fact]
    public void SqlServer_Migrations_Have_A_Current_Model_And_Generate_Provider_Specific_Sql()
    {
        using var db = new SqlServerMigrationContextFactory().CreateDbContext([]);
        Assert.False(db.Database.HasPendingModelChanges());
        var script = db.GetService<IMigrator>()
            .GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("CREATE UNIQUE INDEX [IX_Persons_Username]", script);
        Assert.Contains("nvarchar(320)", script);
        Assert.DoesNotContain("AUTOINCREMENT", script);
    }
}

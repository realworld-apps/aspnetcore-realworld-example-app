using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Conduit.Infrastructure;

public class SqliteMigrationContextFactory : IDesignTimeDbContextFactory<SqliteConduitContext>
{
    public SqliteConduitContext CreateDbContext(string[] args) =>
        new(
            new DbContextOptionsBuilder<SqliteConduitContext>()
                .UseSqlite("Data Source=realworld.db")
                .Options
        );
}

public class SqlServerMigrationContextFactory : IDesignTimeDbContextFactory<SqlServerConduitContext>
{
    public SqlServerConduitContext CreateDbContext(string[] args) =>
        new(
            new DbContextOptionsBuilder<SqlServerConduitContext>()
                .UseSqlServer(
                    "Server=localhost;Database=Conduit;Integrated Security=true;TrustServerCertificate=true"
                )
                .Options
        );
}

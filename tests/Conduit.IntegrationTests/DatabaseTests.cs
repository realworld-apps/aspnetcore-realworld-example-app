using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Conduit.IntegrationTests;

public class DatabaseTests : SliceFixture
{
    [Theory]
    [InlineData("username")]
    [InlineData("email")]
    public async Task Database_Rejects_Duplicate_User_Identity_Without_Handler_Prechecks(
        string field
    )
    {
        await InsertAsync(new Person { Username = "one", Email = "one@example.com" });
        var exception = await Assert.ThrowsAsync<RestException>(() =>
            InsertAsync(
                new Person
                {
                    Username = field == "username" ? "one" : "two",
                    Email = field == "email" ? "one@example.com" : "two@example.com",
                }
            )
        );
        Assert.Equal(HttpStatusCode.Conflict, exception.Code);
    }

    [Fact]
    public async Task Database_Rejects_Duplicate_Article_Slugs()
    {
        await InsertAsync(new Article { Slug = "same" });
        var exception = await Assert.ThrowsAsync<RestException>(() =>
            InsertAsync(new Article { Slug = "same" })
        );
        Assert.Equal(HttpStatusCode.Conflict, exception.Code);
    }

    [Theory]
    [InlineData("sqlite", "Data Source=:memory:", "Microsoft.EntityFrameworkCore.Sqlite")]
    [InlineData(
        "sqlserver",
        "Server=localhost;Database=Conduit;Integrated Security=true",
        "Microsoft.EntityFrameworkCore.SqlServer"
    )]
    public void Configuration_Selects_The_Requested_Provider(
        string provider,
        string connection,
        string expected
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Database:Provider"] = provider,
                    ["ConnectionStrings:Conduit"] = connection,
                }
            )
            .Build();
        var services = new ServiceCollection();
        services.AddConduitDatabase(configuration);
        using var container = services.BuildServiceProvider();
        using var scope = container.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConduitContext>();
        Assert.Equal(expected, db.Database.ProviderName);
        if (provider == "sqlserver")
        {
            var requested = new SqlConnectionStringBuilder(connection);
            var actual = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
            Assert.Equal(requested.DataSource, actual.DataSource);
            Assert.Equal(requested.InitialCatalog, actual.InitialCatalog);
            Assert.Equal(requested.IntegratedSecurity, actual.IntegratedSecurity);
        }
        else
        {
            Assert.Equal(connection, db.Database.GetConnectionString());
        }
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("sqlserver")]
    public void Invalid_Database_Configuration_Fails_Fast(string provider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Database:Provider"] = provider }
            )
            .Build();
        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddConduitDatabase(configuration)
        );
    }
}

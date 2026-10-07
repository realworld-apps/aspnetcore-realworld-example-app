using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Conduit.IntegrationTests;

public class ApiFactory(string prefix = "api") : WebApplicationFactory<Program>
{
    private readonly string _database = Path.Combine(
        Path.GetTempPath(),
        $"conduit-http-{Guid.NewGuid():N}.db"
    );
    private readonly string _key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:SigningKey", _key);
        builder.UseSetting("Database:Provider", "sqlite");
        builder.UseSetting("ConnectionStrings:Conduit", $"Data Source={_database};Pooling=False");
        builder.UseSetting("ApiPrefix", prefix);
        builder.UseSetting("Logging:LogLevel:Default", "Warning");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            File.Delete(_database);
            File.Delete(_database + "-wal");
            File.Delete(_database + "-shm");
        }
    }
}

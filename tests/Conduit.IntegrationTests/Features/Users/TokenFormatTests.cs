using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users;

public class TokenFormatTests
{
    [Theory]
    [InlineData("legacy")]
    [InlineData("collision")]
    [InlineData("version")]
    [InlineData("subject")]
    [InlineData("expired")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("algorithm")]
    [InlineData("unsigned")]
    [InlineData("missing-expiration")]
    public async Task Unsupported_Tokens_Are_Rejected(string scenario)
    {
        var key = RandomNumberGenerator.GetBytes(64);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJwt(
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:SigningKey"] = Convert.ToBase64String(key),
                    }
                )
                .Build()
        );
        services.AddDbContext<ConduitContext>(o =>
            o.UseInMemoryDatabase(Guid.NewGuid().ToString())
        );
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConduitContext>();
        db.Add(new Person { Username = "victim" });
        await db.SaveChangesAsync();
        var claims = new List<Claim>
        {
            new(
                "sub",
                scenario == "legacy" ? "victim"
                    : scenario == "subject" ? "user:0"
                    : "user:1"
            ),
        };
        if (scenario is not ("legacy" or "collision"))
        {
            claims.Add(new Claim("conduit_token_version", scenario == "version" ? "1" : "2"));
        }
        var jwt = new JwtSecurityToken(
            scenario == "issuer" ? "wrong" : "issuer",
            scenario == "audience" ? "wrong" : "audience",
            claims,
            DateTime.UtcNow.AddMinutes(-10),
            scenario == "missing-expiration" ? null
                : scenario == "expired" ? DateTime.UtcNow.AddMinutes(-5)
                : DateTime.UtcNow.AddMinutes(5),
            scenario == "unsigned"
                ? null
                : new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    scenario == "algorithm"
                        ? SecurityAlgorithms.HmacSha512
                        : SecurityAlgorithms.HmacSha256
                )
        );
        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        http.Request.Headers.Authorization =
            "Token " + new JwtSecurityTokenHandler().WriteToken(jwt);
        Assert.False((await http.AuthenticateAsync()).Succeeded);
        await http.ChallengeAsync();
        Assert.Equal(StatusCodes.Status401Unauthorized, http.Response.StatusCode);
    }
}

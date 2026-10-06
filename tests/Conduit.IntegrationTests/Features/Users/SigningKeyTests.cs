using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Conduit.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users;

public class SigningKeyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not base64")]
    [InlineData("c2hvcnQ=")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=")]
    [InlineData("c29tZXRoaW5nbG9uZ2VyZm9ydGhpc2R1bWJhbGdvcml0aG1pc3JlcXVpcmVk")]
    [InlineData("c29tZXRoaW5nbG9uZ2VyZm9ydGhpc2R1bWJhbGdvcml0aG1pc3JlcXVpcmVkMQ==")]
    [InlineData("MXNvbWV0aGluZ2xvbmdlcmZvcnRoaXNkdW1iYWxnb3JpdGhtaXNyZXF1aXJlZA==")]
    public void Invalid_Keys_Are_Rejected(string? key)
    {
        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddJwt(Configuration(key))
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Tokens_From_Another_Deployment_Or_Public_Key_Are_Rejected(bool publicKey)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConduit();
        services.AddJwt(Configuration(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));
        await using var issuer = services.BuildServiceProvider();
        await using var scope = issuer.CreateAsyncScope();
        var token = scope
            .ServiceProvider.GetRequiredService<IJwtTokenGenerator>()
            .CreateToken("user");
        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        http.Request.Headers.Authorization = "Token " + token;
        Assert.True((await http.AuthenticateAsync()).Succeeded);

        var other = new ServiceCollection();
        other.AddLogging();
        other.AddJwt(Configuration(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));
        await using var validator = other.BuildServiceProvider();
        var invalidHttp = new DefaultHttpContext { RequestServices = validator };
        var forged = new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(
                "issuer",
                "audience",
                [new Claim("sub", "user")],
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(5),
                new SigningCredentials(
                    new SymmetricSecurityKey(
                        "somethinglongerforthisdumbalgorithmisrequired"u8.ToArray()
                    ),
                    SecurityAlgorithms.HmacSha256
                )
            )
        );
        invalidHttp.Request.Headers.Authorization = "Token " + (publicKey ? forged : token);
        Assert.False((await invalidHttp.AuthenticateAsync()).Succeeded);
        await invalidHttp.ChallengeAsync();
        Assert.Equal(StatusCodes.Status401Unauthorized, invalidHttp.Response.StatusCode);
    }

    private static IConfiguration Configuration(string? key) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = key })
            .Build();
}

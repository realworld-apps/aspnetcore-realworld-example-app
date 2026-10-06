using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users;

public class TokenIdentityTests
{
    [Fact]
    public async Task Token_Survives_Rename_And_Cannot_Identify_Reused_Username()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConduit();
        services.AddJwt(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(
                    new System.Collections.Generic.Dictionary<string, string?>
                    {
                        ["Jwt:SigningKey"] = Convert.ToBase64String(
                            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)
                        ),
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
        var person = new Person { Username = "before" };
        db.Add(person);
        await db.SaveChangesAsync();
        var token = scope
            .ServiceProvider.GetRequiredService<IJwtTokenGenerator>()
            .CreateToken(person.PersonId);
        person.Username = "after";
        db.Add(new Person { Username = "before" });
        await db.SaveChangesAsync();
        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        http.Request.Headers.Authorization = "Token " + token;
        var result = await http.AuthenticateAsync();
        Assert.True(result.Succeeded);
        Assert.Equal("user:" + person.PersonId, result.Principal.FindFirstValue("sub"));
        http.User = result.Principal;
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentUserAccessor>();
        Assert.Equal(person.PersonId, accessor.GetCurrentPersonId());
        var article = await new Conduit.Features.Articles.Create.Handler(db, accessor).Handle(
            new Conduit.Features.Articles.Create.Command(
                new Conduit.Features.Articles.Create.ArticleData
                {
                    Title = "After rename",
                    Description = "d",
                    Body = "b",
                }
            ),
            default
        );
        Assert.Equal("after", article.Article.Author?.Username);
        Assert.Equal(person.PersonId, article.Article.Author?.PersonId);
        db.Remove(person);
        await db.SaveChangesAsync();
        await using var deletedScope = provider.CreateAsyncScope();
        var deletedHttp = new DefaultHttpContext { RequestServices = deletedScope.ServiceProvider };
        deletedHttp.Request.Headers.Authorization = "Token " + token;
        Assert.False((await deletedHttp.AuthenticateAsync()).Succeeded);
        await deletedHttp.ChallengeAsync();
        Assert.Equal(StatusCodes.Status401Unauthorized, deletedHttp.Response.StatusCode);
    }
}

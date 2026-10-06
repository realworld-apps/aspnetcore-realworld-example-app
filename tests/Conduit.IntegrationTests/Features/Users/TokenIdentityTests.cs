using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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
        services.AddJwt();
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
        Assert.Equal("after", result.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
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

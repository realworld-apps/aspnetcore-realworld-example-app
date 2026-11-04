using System;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Features.Users;
using Conduit.Infrastructure.Security;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users;

public class LoginTests : SliceFixture
{
    [Fact]
    public async Task Expect_Login()
    {
        var salt = Guid.NewGuid().ToByteArray();
        var person = new Person
        {
            Username = "username",
            Email = "user@example.com",
            Hash = await new PasswordHasher().Hash("password", salt),
            Salt = salt,
        };
        await InsertAsync(person);

        var command = new Login.Command(
            new Login.UserData { Email = "user@example.com", Password = "password" }
        );

        var user = await SendAsync(command);

        Assert.NotNull(user?.User);
        Assert.Equal(user.User.Email, command.User.Email);
        Assert.Equal("username", user.User.Username);
        Assert.NotNull(user.User.Token);
    }
}

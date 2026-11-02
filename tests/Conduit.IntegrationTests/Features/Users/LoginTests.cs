using System;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Features.Users;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using FluentValidation;
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
            Email = "email@example.com",
            Hash = await new PasswordHasher().Hash("password", salt),
            Salt = salt,
        };
        await InsertAsync(person);

        var command = new Login.Command(
            new Login.UserData { Email = "email@example.com", Password = "password" }
        );

        var user = await SendAsync(command);

        Assert.NotNull(user?.User);
        Assert.Equal(user.User.Email, command.User.Email);
        Assert.Equal("username", user.User.Username);
        Assert.NotNull(user.User.Token);
    }

    [Theory]
    [InlineData("no-at-sign")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    public async Task Expect_Login_With_Malformed_Email_Is_Invalid(string email)
    {
        var command = new Login.Command(
            new Login.UserData { Email = email, Password = "password" }
        );

        var ex = await Assert.ThrowsAsync<ValidationException>(() => SendAsync(command).AsTask());

        var error = Assert.Single(ex.Errors);
        Assert.Equal("User.Email", error.PropertyName);
        Assert.Equal(Constants.INVALID, error.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Expect_Login_With_Blank_Email_Is_Blank_Only(string? email)
    {
        var command = new Login.Command(
            new Login.UserData { Email = email, Password = "password" }
        );

        var ex = await Assert.ThrowsAsync<ValidationException>(() => SendAsync(command).AsTask());

        var error = Assert.Single(ex.Errors);
        Assert.Equal("User.Email", error.PropertyName);
        Assert.Equal(Constants.BLANK, error.ErrorMessage);
    }
}

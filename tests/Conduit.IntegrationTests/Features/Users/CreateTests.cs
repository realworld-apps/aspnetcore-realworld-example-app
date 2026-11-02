using System.Linq;
using System.Threading.Tasks;
using Conduit.Features.Users;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users;

public class CreateTests : SliceFixture
{
    [Fact]
    public async Task Expect_Create_User()
    {
        var command = new Create.Command(
            new Create.UserData("username", "email@example.com", "password")
        );

        await SendAsync(command);

        var created = await ExecuteDbContextAsync(db =>
            db.Persons.Where(d => d.Email == command.User.Email).SingleOrDefaultAsync()
        );

        Assert.NotNull(created);
        using var hasher = new PasswordHasher();
        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.Verify("password", created.Hash, created.Salt)
        );
        Assert.Empty(created.Salt);
    }

    [Theory]
    [InlineData("no-at-sign")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    public async Task Expect_Create_User_With_Malformed_Email_Is_Invalid(string email)
    {
        var command = new Create.Command(new Create.UserData("username", email, "password"));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => SendAsync(command).AsTask());

        var error = Assert.Single(ex.Errors);
        Assert.Equal("User.Email", error.PropertyName);
        Assert.Equal(Constants.INVALID, error.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Expect_Create_User_With_Blank_Email_Is_Blank_Only(string? email)
    {
        var command = new Create.Command(new Create.UserData("username", email, "password"));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => SendAsync(command).AsTask());

        var error = Assert.Single(ex.Errors);
        Assert.Equal("User.Email", error.PropertyName);
        Assert.Equal(Constants.BLANK, error.ErrorMessage);
    }
}

using System.Net;
using System.Threading.Tasks;
using Conduit.Features;
using Conduit.Features.Users;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users;

public class EditTests : SliceFixture
{
    private Edit.Handler CreateHandler(string currentUser) =>
        new(
            GetDbContext(),
            new PasswordHasher(),
            new StubCurrentUserAccessor(currentUser),
            new StubJwtTokenGenerator(),
            new ConduitMapper()
        );

    [Fact]
    public async Task Expect_Edit_User_Username_To_Duplicate_Throws_Conflict()
    {
        await SendAsync(
            new Create.Command(new Create.UserData("user1", "user1@example.com", "password1"))
        );
        await SendAsync(
            new Create.Command(new Create.UserData("user2", "user2@example.com", "password2"))
        );

        var userData = new Edit.UserData();
        userData.Username = "user1";

        var ex = await Assert.ThrowsAsync<RestException>(() =>
            CreateHandler("user2").Handle(new Edit.Command(userData), default).AsTask()
        );

        Assert.Equal(HttpStatusCode.Conflict, ex.Code);
    }

    [Fact]
    public async Task Expect_Edit_User_Email_To_Duplicate_Throws_Conflict()
    {
        await SendAsync(
            new Create.Command(new Create.UserData("user3", "user3@example.com", "password3"))
        );
        await SendAsync(
            new Create.Command(new Create.UserData("user4", "user4@example.com", "password4"))
        );

        var userData = new Edit.UserData();
        userData.Email = "user3@example.com";

        var ex = await Assert.ThrowsAsync<RestException>(() =>
            CreateHandler("user4").Handle(new Edit.Command(userData), default).AsTask()
        );

        Assert.Equal(HttpStatusCode.Conflict, ex.Code);
    }

    [Fact]
    public async Task Expect_Edit_User_Same_Username_Does_Not_Throw()
    {
        await SendAsync(
            new Create.Command(new Create.UserData("user5", "user5@example.com", "password5"))
        );

        var userData = new Edit.UserData();
        userData.Username = "user5";

        var result = await CreateHandler("user5").Handle(new Edit.Command(userData), default);

        Assert.Equal("user5", result.User.Username);
    }

    [Fact]
    public async Task Expect_Edit_User_Same_Email_Does_Not_Throw()
    {
        await SendAsync(
            new Create.Command(new Create.UserData("user6", "user6@example.com", "password6"))
        );

        var userData = new Edit.UserData();
        userData.Email = "user6@example.com";

        var result = await CreateHandler("user6").Handle(new Edit.Command(userData), default);

        Assert.Equal("user6@example.com", result.User.Email);
    }
}

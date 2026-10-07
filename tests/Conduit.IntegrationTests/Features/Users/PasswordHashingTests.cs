using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Features.Users;
using Conduit.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users;

public class PasswordHashingTests : SliceFixture
{
    [Fact]
    public async Task New_Hashes_Are_Randomized_And_Verify_Only_The_Right_Password()
    {
        using var hasher = new PasswordHasher();
        var first = await hasher.Hash("password", []);
        var second = await hasher.Hash("password", []);
        Assert.NotEqual(first, second);
        Assert.Equal(PasswordVerificationResult.Success, hasher.Verify("password", first, []));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.Verify("wrong", first, []));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.Verify("password", [], []));
    }

    [Fact]
    public async Task Legacy_Password_Is_Rehashed_Only_After_A_Successful_Login()
    {
        var salt = Guid.NewGuid().ToByteArray();
        var input = Encoding.UTF8.GetBytes("password").Concat(salt).ToArray();
        var legacyHash = HMACSHA512.HashData(Encoding.UTF8.GetBytes("realworld"), input);
        await InsertAsync(
            new Person
            {
                Username = "legacy",
                Email = "legacy@example.com",
                Hash = legacyHash,
                Salt = salt,
            }
        );

        await Assert.ThrowsAsync<Infrastructure.Errors.RestException>(() =>
            SendAsync(
                    new Login.Command(
                        new Login.UserData { Email = "legacy@example.com", Password = "wrong" }
                    )
                )
                .AsTask()
        );
        var unchanged = await ExecuteDbContextAsync(db => db.Persons.SingleAsync());
        Assert.Equal(legacyHash, unchanged.Hash);

        await SendAsync(
            new Login.Command(
                new Login.UserData { Email = "legacy@example.com", Password = "password" }
            )
        );
        var upgraded = await ExecuteDbContextAsync(db => db.Persons.SingleAsync());
        Assert.NotEqual(legacyHash, upgraded.Hash);
        Assert.Empty(upgraded.Salt);
        using var hasher = new PasswordHasher();
        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.Verify("password", upgraded.Hash, upgraded.Salt)
        );
        await SendAsync(
            new Login.Command(
                new Login.UserData { Email = "legacy@example.com", Password = "password" }
            )
        );
    }
}

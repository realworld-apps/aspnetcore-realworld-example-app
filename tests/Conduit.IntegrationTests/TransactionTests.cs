using System;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Features.Articles;
using Conduit.Features.Users;
using Conduit.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;
using UserCreate = Conduit.Features.Users.Create;

namespace Conduit.IntegrationTests;

public class TransactionTests : SliceFixture
{
    [Fact]
    public async Task Queries_Do_Not_Start_A_Transaction()
    {
        var db = GetDbContext();
        var pipeline = new DBContextTransactionPipelineBehavior<List.Query, ArticlesEnvelope>(db);
        await pipeline.Handle(
            new List.Query(null, null, null, null, null),
            (_, _) =>
            {
                Assert.Null(db.Database.CurrentTransaction);
                return ValueTask.FromResult(new ArticlesEnvelope());
            },
            CancellationToken.None
        );
    }

    [Fact]
    public async Task Failed_Commands_Roll_Back_Partial_Writes()
    {
        var db = GetDbContext();
        var pipeline = new DBContextTransactionPipelineBehavior<UserCreate.Command, UserEnvelope>(
            db
        );
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline
                .Handle(
                    new UserCreate.Command(
                        new UserCreate.UserData("partial", "partial@example.com", "password")
                    ),
                    async (_, cancellationToken) =>
                    {
                        Assert.NotNull(db.Database.CurrentTransaction);
                        db.Add(new Person { Username = "partial" });
                        await db.SaveChangesAsync(cancellationToken);
                        throw new InvalidOperationException("Failure after the first save.");
                    },
                    CancellationToken.None
                )
                .AsTask()
        );
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Equal(0, await db.Persons.CountAsync());
    }
}

using System.Threading;
using System.Threading.Tasks;
using Conduit.Features.Articles;
using Xunit;

namespace Conduit.IntegrationTests.Features.Articles;

public class ListTests : SliceFixture
{
    [Fact]
    public async Task Expect_List_Articles_No_Params()
    {
        var command = new Create.Command(
            new Create.ArticleData
            {
                Title = "Test article list",
                Description = "Description",
                Body = "Body",
                TagList = ["tag1"],
            }
        );
        await ArticleHelpers.CreateArticle(this, command);

        var result = await SendAsync(new List.Query(null, null, null, null, null));

        Assert.NotNull(result);
        Assert.True(result.ArticlesCount > 0);
    }

    [Fact]
    public async Task Expect_List_Articles_With_Tag_Filter()
    {
        var command = new Create.Command(
            new Create.ArticleData
            {
                Title = "Test article tag filter",
                Description = "Description",
                Body = "Body",
                TagList = ["uniquetag123"],
            }
        );
        await ArticleHelpers.CreateArticle(this, command);

        var result = await SendAsync(new List.Query("uniquetag123", null, null, null, null));

        Assert.NotNull(result);
        Assert.True(result.ArticlesCount > 0);
    }

    [Fact]
    public async Task Expect_List_Articles_With_Nonexistent_Tag_Returns_Empty()
    {
        var result = await SendAsync(new List.Query("nonexistenttag99999", null, null, null, null));

        Assert.NotNull(result);
        Assert.Equal(0, result.ArticlesCount);
    }

    [Fact]
    public async Task Expect_List_Articles_With_Limit_And_Offset()
    {
        var user = await Users.UserHelpers.CreateDefaultUser(this);
        var dbContext = GetDbContext();
        var currentAccessor = new StubCurrentUserAccessor(user.Username!);
        var handler = new Create.Handler(dbContext, currentAccessor);
        for (var i = 0; i < 3; i++)
        {
            await handler.Handle(
                new Create.Command(
                    new Create.ArticleData
                    {
                        Title = $"Test article pagination {i}",
                        Description = "Description",
                        Body = "Body",
                    }
                ),
                CancellationToken.None
            );
        }

        var result = await SendAsync(new List.Query(null, null, null, 2, 0));

        Assert.NotNull(result);
        Assert.Equal(2, result.Articles.Count);
    }

    [Fact]
    public async Task Expect_Feed_With_No_Params_Returns_Empty_For_User_With_No_Follows()
    {
        var user = await Users.UserHelpers.CreateDefaultUser(this);
        var dbContext = GetDbContext();
        var currentAccessor = new StubCurrentUserAccessor(user.Username!);
        var handler = new List.QueryHandler(dbContext, currentAccessor);

        var result = await handler.Handle(
            new List.Query(null, null, null, null, null) { IsFeed = true },
            CancellationToken.None
        );

        Assert.NotNull(result);
        Assert.Equal(0, result.ArticlesCount);
    }
}

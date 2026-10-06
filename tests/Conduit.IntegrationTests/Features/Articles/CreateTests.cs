using System.Linq;
using System.Threading.Tasks;
using Conduit.Features.Articles;
using Xunit;

namespace Conduit.IntegrationTests.Features.Articles;

public class CreateTests : SliceFixture
{
    [Fact]
    public async Task Duplicate_Tags_Are_Ignored_For_New_And_Existing_Tags()
    {
        var article = await ArticleHelpers.CreateArticle(
            this,
            new Create.Command(
                new Create.ArticleData
                {
                    Title = "Duplicates",
                    Description = "d",
                    Body = "b",
                    TagList = ["x", "x"],
                }
            )
        );
        Assert.Equal("x", Assert.Single(article.TagList));
        var handler = new Create.Handler(
            GetDbContext(),
            new StubCurrentUserAccessor(Users.UserHelpers.DefaultUserName)
        );
        var result = await handler.Handle(
            new Create.Command(
                new Create.ArticleData
                {
                    Title = "Existing duplicates",
                    Description = "d",
                    Body = "b",
                    TagList = ["x", "x"],
                }
            ),
            default
        );
        Assert.Equal("x", Assert.Single(result.Article.TagList));
    }

    [Fact]
    public async Task Expect_Create_Article()
    {
        var command = new Create.Command(
            new Create.ArticleData
            {
                Title = "Test article dsergiu77",
                Description = "Description of the test article",
                Body = "Body of the test article",
                TagList = ["tag1", "tag2"],
            }
        );

        var article = await ArticleHelpers.CreateArticle(this, command);

        Assert.NotNull(article);
        Assert.Equal(article.Title, command.Article.Title);
        Assert.Equal(article.TagList.Count(), command.Article.TagList?.Count());
    }
}

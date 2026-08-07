using System;
using System.Linq;
using System.Threading.Tasks;
using Conduit.Features.Articles;
using Conduit.IntegrationTests.Features.Users;
using Xunit;

namespace Conduit.IntegrationTests.Features.Articles;

public class EditTests : SliceFixture
{
    [Fact]
    public async Task Expect_Edit_Article()
    {
        var createCommand = new Create.Command(
            new Create.ArticleData
            {
                Title = "Test article dsergiu77",
                Description = "Description of the test article",
                Body = "Body of the test article",
                TagList = ["tag1", "tag2"],
            }
        );

        var createdArticle = await ArticleHelpers.CreateArticle(this, createCommand);

        var command = new Edit.Command(
            new(
                new Edit.ArticleData
                {
                    Title = "Updated " + createdArticle.Title,
                    Description = "Updated " + createdArticle.Description,
                    Body = "Updated " + createdArticle.Body,
                    TagList = [createdArticle.TagList[1], "tag3"],
                }
            ),
            createdArticle.Slug ?? throw new InvalidOperationException()
        );

        var dbContext = GetDbContext();

        var articleEditHandler = new Edit.Handler(
            dbContext,
            new StubCurrentUserAccessor(UserHelpers.DefaultUserName)
        );
        var edited = await articleEditHandler.Handle(
            command,
            new System.Threading.CancellationToken()
        );

        Assert.NotNull(edited);
        Assert.Equal(edited.Article.Title, command.Model.Article.Title);
        Assert.Equal(edited.Article.TagList.Count, command.Model.Article.TagList?.Count() ?? 0);
        // use assert Contains because we do not know the order in which the tags are saved/retrieved
        Assert.Contains(edited.Article.TagList[0], command.Model.Article.TagList ?? []);
        Assert.Contains(edited.Article.TagList[1], command.Model.Article.TagList ?? []);
    }
}

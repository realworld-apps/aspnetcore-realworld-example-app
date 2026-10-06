using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Features.Articles;
using Xunit;

namespace Conduit.IntegrationTests.Features.Articles;

public class FavoritedTests : SliceFixture
{
    [Theory]
    [InlineData("fan", true)]
    [InlineData("author", false)]
    [InlineData("bystander", false)]
    [InlineData(null, false)]
    public async Task Favorited_Is_Viewer_Specific(string? username, bool expected)
    {
        var db = GetDbContext();
        var author = new Person { Username = "author" };
        var fan = new Person { Username = "fan" };
        var article = new Article { Slug = "article", Author = author };
        db.AddRange(article, fan, new Person { Username = "bystander" });
        db.Add(new ArticleFavorite { Article = article, Person = fan });
        await db.SaveChangesAsync();
        var accessor = new StubCurrentUserAccessor(username);
        var details = await new Details.QueryHandler(db, accessor).Handle(
            new Details.Query("article"),
            default
        );
        Assert.Equal(expected, details.Article.Favorited);
        Assert.Equal(1, details.Article.FavoritesCount);
        var list = await new List.QueryHandler(db, accessor).Handle(
            new List.Query(null, null, null, null, null),
            default
        );
        Assert.Equal(expected, Assert.Single(list.Articles).Favorited);
        Assert.Equal(1, Assert.Single(list.Articles).FavoritesCount);
    }
}

using System;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Features.Articles;
using Conduit.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Conduit.IntegrationTests.Features.Articles;

public class TagPersistenceTests
{
    [Fact]
    public async Task Edit_Inserts_New_Tags_And_Reuses_Existing_Tags()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ConduitContext>().UseSqlite(connection).Options;
        await using var context = new ConduitContext(options);
        await context.Database.EnsureCreatedAsync();
        var author = new Person { Username = "author" };
        var article = new Article
        {
            Slug = "article",
            Author = author,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.AddRange(article, new Tag { TagId = "existing" });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var handler = new Edit.Handler(context, new StubCurrentUserAccessor("author"));
        var result = await handler.Handle(
            new Edit.Command(
                new(new Edit.ArticleData { TagList = ["existing", "new"] }),
                "article"
            ),
            default
        );
        Assert.Equal(2, result.Article.TagList.Count);
        context.ChangeTracker.Clear();
        Assert.Equal(2, await context.Tags.CountAsync());
        Assert.Equal(2, await context.ArticleTags.CountAsync());
    }
}

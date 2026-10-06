using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Articles;

public static class ArticleViewerExtensions
{
    public static async Task PopulateFavoritedAsync(
        this IEnumerable<Article> articles,
        ConduitContext context,
        string? username,
        CancellationToken cancellationToken
    )
    {
        var personId = username is null
            ? null
            : await context
                .Persons.Where(x => x.Username == username)
                .Select(x => (int?)x.PersonId)
                .SingleOrDefaultAsync(cancellationToken);
        foreach (var article in articles)
        {
            article.Favorited =
                personId.HasValue
                && article.ArticleFavorites.Any(x => x.PersonId == personId.Value);
        }
    }
}

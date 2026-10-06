using System.Collections.Generic;
using System.Linq;
using Conduit.Domain;

namespace Conduit.Features.Articles;

public static class ArticleViewerExtensions
{
    public static void PopulateFavorited(this IEnumerable<Article> articles, int? personId)
    {
        foreach (var article in articles)
        {
            article.Favorited =
                personId.HasValue
                && article.ArticleFavorites.Any(x => x.PersonId == personId.Value);
        }
    }
}

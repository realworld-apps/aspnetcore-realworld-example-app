using System.Collections.Generic;

namespace Conduit.Features.Articles;

public class ArticlesEnvelope
{
    public List<ArticleSummary> Articles { get; set; } = new();

    public int ArticlesCount { get; set; }
}

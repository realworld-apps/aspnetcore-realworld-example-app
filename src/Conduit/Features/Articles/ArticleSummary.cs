using System;
using System.Collections.Generic;
using Conduit.Features.Profiles;

namespace Conduit.Features.Articles;

public class ArticleSummary
{
    public string? Slug { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public List<string> TagList { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public bool Favorited { get; init; }
    public int FavoritesCount { get; init; }
    public Profile? Author { get; init; }
}

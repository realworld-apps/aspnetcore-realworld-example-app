using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Articles;

public class Edit
{
    public record ArticleData(string? Title, string? Description, string? Body, string[]? TagList);

    public record Command(Model Model, string Slug) : IRequest<ArticleEnvelope>;

    public record Model(ArticleData Article);

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator() => RuleFor(x => x.Model.Article).NotNull();
    }

    public class Handler(ConduitContext context) : IRequestHandler<Command, ArticleEnvelope>
    {
        public async Task<ArticleEnvelope> Handle(
            Command message,
            CancellationToken cancellationToken
        )
        {
            var article = await context
                .Articles.Include(x => x.ArticleTags) // include also the article tags since they also need to be updated
                .Where(x => x.Slug == message.Slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (article == null)
            {
                throw new RestException(
                    HttpStatusCode.NotFound,
                    new { Article = Constants.NOT_FOUND }
                );
            }

            article.Description = message.Model.Article.Description ?? article.Description;
            article.Body = message.Model.Article.Body ?? article.Body;
            article.Title = message.Model.Article.Title ?? article.Title;
            article.Slug = article.Title.GenerateSlug();

            // list of currently saved article tags for the given article
            var articleTagList = message.Model.Article.TagList ?? Enumerable.Empty<string>();

            var articleTagsToCreate = GetArticleTagsToCreate(article, articleTagList);
            var articleTagsToDelete = GetArticleTagsToDelete(article, articleTagList);

            if (
                context.ChangeTracker.Entries().First(x => x.Entity == article).State
                    == EntityState.Modified
                || articleTagsToCreate.Count != 0
                || articleTagsToDelete.Count != 0
            )
            {
                article.UpdatedAt = DateTime.UtcNow;
            }

            // ensure context is tracking any tags that are about to be created so that it won't attempt to insert a duplicate
            context.Tags.AttachRange(
                [.. articleTagsToCreate.Where(x => x.Tag is not null).Select(a => a.Tag!)]
            );

            // add the new article tags
            await context.ArticleTags.AddRangeAsync(articleTagsToCreate, cancellationToken);

            // delete the tags that do not exist anymore
            context.ArticleTags.RemoveRange(articleTagsToDelete);

            await context.SaveChangesAsync(cancellationToken);

            article = await context
                .Articles.GetAllData()
                .Where(x => x.Slug == article.Slug)
                .FirstOrDefaultAsync(x => x.ArticleId == article.ArticleId, cancellationToken);
            if (article is null)
            {
                throw new RestException(
                    HttpStatusCode.NotFound,
                    new { Article = Constants.NOT_FOUND }
                );
            }

            return new ArticleEnvelope(article);
        }

        /// <summary>
        /// check which article tags need to be added
        /// </summary>
        private static List<ArticleTag> GetArticleTagsToCreate(
            Article article,
            IEnumerable<string> articleTagList
        )
        {
            var articleTagsToCreate = new List<ArticleTag>();
            foreach (var tag in articleTagList)
            {
                var at = article.ArticleTags?.FirstOrDefault(t => t.TagId == tag);
                if (at == null)
                {
                    at = new ArticleTag
                    {
                        Article = article,
                        ArticleId = article.ArticleId,
                        Tag = new Tag { TagId = tag },
                        TagId = tag,
                    };
                    articleTagsToCreate.Add(at);
                }
            }

            return articleTagsToCreate;
        }

        /// <summary>
        /// check which article tags need to be deleted
        /// </summary>
        private static List<ArticleTag> GetArticleTagsToDelete(
            Article article,
            IEnumerable<string> articleTagList
        )
        {
            var articleTagsToDelete = new List<ArticleTag>();
            foreach (var tag in article.ArticleTags)
            {
                var at = articleTagList.FirstOrDefault(t => t == tag.TagId);
                if (at == null)
                {
                    articleTagsToDelete.Add(tag);
                }
            }

            return articleTagsToDelete;
        }
    }
}

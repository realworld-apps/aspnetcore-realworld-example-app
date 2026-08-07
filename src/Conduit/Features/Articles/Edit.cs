using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Articles;

public class Edit
{
    // the setter records whether tagList was present in the request body: the RealWorld spec
    // preserves tags when the field is absent, clears them on [], and rejects an explicit null
    public class ArticleData
    {
        private string[]? _tagList;

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? Body { get; set; }

        public string[]? TagList
        {
            get => _tagList;
            set
            {
                _tagList = value;
                TagListSet = true;
            }
        }

        [JsonIgnore]
        public bool TagListSet { get; private set; }
    }

    public record Command(Model Model, string Slug) : IRequest<ArticleEnvelope>;

    public record Model(ArticleData Article);

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Model.Article).NotNull();
            RuleFor(x => x.Model.Article.TagList)
                .NotNull()
                .When(x => x.Model.Article is { TagListSet: true });
        }
    }

    public class Handler(ConduitContext context, ICurrentUserAccessor currentUserAccessor)
        : IRequestHandler<Command, ArticleEnvelope>
    {
        public async ValueTask<ArticleEnvelope> Handle(
            Command message,
            CancellationToken cancellationToken
        )
        {
            var article = await context
                .Articles.Include(x => x.ArticleTags) // include also the article tags since they also need to be updated
                .Include(x => x.Author)
                .Where(x => x.Slug == message.Slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (article == null)
            {
                throw new RestException(HttpStatusCode.NotFound, "article", Constants.NOT_FOUND);
            }

            if (article.Author?.Username != currentUserAccessor.GetCurrentUsername())
            {
                throw new RestException(HttpStatusCode.Forbidden, "article", Constants.FORBIDDEN);
            }

            article.Description = message.Model.Article.Description ?? article.Description;
            article.Body = message.Model.Article.Body ?? article.Body;
            if (
                !string.IsNullOrEmpty(message.Model.Article.Title)
                && message.Model.Article.Title != article.Title
            )
            {
                article.Title = message.Model.Article.Title;
                var slug = article.Title.GenerateSlug();
                // keep slugs unique when the new title collides with an existing article
                var uniqueSlug = slug;
                for (
                    var i = 1;
                    await context.Articles.AnyAsync(
                        x => x.Slug == uniqueSlug && x.ArticleId != article.ArticleId,
                        cancellationToken
                    );
                    i++
                )
                {
                    uniqueSlug = $"{slug}-{i}";
                }
                article.Slug = uniqueSlug;
            }

            // when tagList is absent from the request the current tags are preserved
            var articleTagList = message.Model.Article.TagListSet
                ? (message.Model.Article.TagList ?? [])
                : article.ArticleTags.Where(x => x.TagId is not null).Select(x => x.TagId!);

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
            context.Tags.AttachRange([
                .. articleTagsToCreate.Where(x => x.Tag is not null).Select(a => a.Tag!),
            ]);

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
                throw new RestException(HttpStatusCode.NotFound, "article", Constants.NOT_FOUND);
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

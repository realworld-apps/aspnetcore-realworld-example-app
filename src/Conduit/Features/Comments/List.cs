using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Comments;

public class List
{
    public record Query(string Slug) : IRequest<CommentsEnvelope>;

    public class QueryHandler(ConduitContext context) : IRequestHandler<Query, CommentsEnvelope>
    {
        public async ValueTask<CommentsEnvelope> Handle(
            Query message,
            CancellationToken cancellationToken
        )
        {
            var article = await context
                .Articles.Include(x => x.Comments)
                    .ThenInclude(x => x.Author)
                .FirstOrDefaultAsync(x => x.Slug == message.Slug, cancellationToken);

            if (article == null)
            {
                throw new RestException(HttpStatusCode.NotFound, "article", Constants.NOT_FOUND);
            }

            return new CommentsEnvelope(article.Comments);
        }
    }
}

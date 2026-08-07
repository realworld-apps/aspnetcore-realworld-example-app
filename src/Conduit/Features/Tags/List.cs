using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Tags;

public class List
{
    public record Query : IRequest<TagsEnvelope>;

    public class QueryHandler(ConduitContext context) : IRequestHandler<Query, TagsEnvelope>
    {
        public async ValueTask<TagsEnvelope> Handle(
            Query message,
            CancellationToken cancellationToken
        )
        {
            var tags = await context
                .Tags.OrderBy(x => x.TagId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            return new TagsEnvelope
            {
                Tags = tags?.Select(x => x.TagId ?? string.Empty).ToList() ?? new List<string>(),
            };
        }
    }
}

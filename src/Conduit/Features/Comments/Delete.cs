using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Comments;

public class Delete
{
    public record Command(string Slug, int Id) : IRequest;

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator() => RuleFor(x => x.Slug).NotNull().NotEmpty();
    }

    public class QueryHandler(ConduitContext context, ICurrentUserAccessor currentUserAccessor)
        : IRequestHandler<Command>
    {
        public async ValueTask<Unit> Handle(Command message, CancellationToken cancellationToken)
        {
            var article =
                await context
                    .Articles.Include(x => x.Comments)
                        .ThenInclude(x => x.Author)
                    .FirstOrDefaultAsync(x => x.Slug == message.Slug, cancellationToken)
                ?? throw new RestException(HttpStatusCode.NotFound, "article", Constants.NOT_FOUND);

            var comment =
                article.Comments.FirstOrDefault(x => x.CommentId == message.Id)
                ?? throw new RestException(HttpStatusCode.NotFound, "comment", Constants.NOT_FOUND);

            if (comment.Author?.Username != currentUserAccessor.GetCurrentUsername())
            {
                throw new RestException(HttpStatusCode.Forbidden, "comment", Constants.FORBIDDEN);
            }

            context.Comments.Remove(comment);
            await context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}

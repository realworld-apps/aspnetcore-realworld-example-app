using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Mediator;

namespace Conduit.Features.Profiles;

public class Details
{
    public record Query(string Username) : IRequest<ProfileEnvelope>;

    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator() => RuleFor(x => x.Username).NotEmpty();
    }

    public class QueryHandler(IProfileReader profileReader)
        : IRequestHandler<Query, ProfileEnvelope>
    {
        public async ValueTask<ProfileEnvelope> Handle(
            Query message,
            CancellationToken cancellationToken
        ) => await profileReader.ReadProfile(message.Username, cancellationToken);
    }
}

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Users;

public class Details
{
    public record Query(int PersonId) : IRequest<UserEnvelope>, IReadOnlyRequest;

    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator() => RuleFor(x => x.PersonId).GreaterThan(0);
    }

    public class QueryHandler(
        ConduitContext context,
        IJwtTokenGenerator jwtTokenGenerator,
        ConduitMapper mapper
    ) : IRequestHandler<Query, UserEnvelope>
    {
        public async ValueTask<UserEnvelope> Handle(
            Query message,
            CancellationToken cancellationToken
        )
        {
            var person = await context
                .Persons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.PersonId == message.PersonId, cancellationToken);

            if (person == null)
            {
                throw new RestException(HttpStatusCode.NotFound, "user", Constants.NOT_FOUND);
            }

            var user = mapper.PersonToUser(person);
            user.Token = jwtTokenGenerator.CreateToken(person.PersonId);
            return new UserEnvelope(user);
        }
    }
}

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Users;

public class Create
{
    public record UserData(string? Username, string? Email, string? Password);

    public record Command(UserData User) : IRequest<UserEnvelope>;

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.User.Username).NotEmpty().WithMessage(Constants.BLANK);
            RuleFor(x => x.User.Email).NotEmpty().WithMessage(Constants.BLANK);
            RuleFor(x => x.User.Password)
                .NotEmpty()
                .WithMessage(Constants.BLANK)
                .MinimumLength(8)
                .WithMessage(Constants.PASSWORD_TOO_SHORT);
        }
    }

    public class Handler(
        ConduitContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ConduitMapper mapper
    ) : IRequestHandler<Command, UserEnvelope>
    {
        public async ValueTask<UserEnvelope> Handle(
            Command message,
            CancellationToken cancellationToken
        )
        {
            if (
                await context
                    .Persons.Where(x => x.Username == message.User.Username)
                    .AnyAsync(cancellationToken)
            )
            {
                throw new RestException(HttpStatusCode.Conflict, "username", Constants.IN_USE);
            }

            if (
                await context
                    .Persons.Where(x => x.Email == message.User.Email)
                    .AnyAsync(cancellationToken)
            )
            {
                throw new RestException(HttpStatusCode.Conflict, "email", Constants.IN_USE);
            }

            var salt = Guid.NewGuid().ToByteArray();
            var person = new Person
            {
                Username = message.User.Username,
                Email = message.User.Email,
                Hash = await passwordHasher.Hash(
                    message.User.Password ?? throw new InvalidOperationException(),
                    salt
                ),
                Salt = salt,
            };

            await context.Persons.AddAsync(person, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var user = mapper.PersonToUser(person);
            user.Token = jwtTokenGenerator.CreateToken(
                person.Username ?? throw new InvalidOperationException()
            );
            return new UserEnvelope(user);
        }
    }
}

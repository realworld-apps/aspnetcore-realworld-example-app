using System;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Users;

public class Edit
{
    // setters record which fields were present in the request body: the RealWorld spec
    // distinguishes an absent field (keep current value) from an explicit null (reject,
    // or clear for the nullable bio/image fields)
    public class UserData
    {
        private string? _username;
        private string? _email;
        private string? _password;
        private string? _bio;
        private string? _image;

        public string? Username
        {
            get => _username;
            set
            {
                _username = value;
                UsernameSet = true;
            }
        }

        public string? Email
        {
            get => _email;
            set
            {
                _email = value;
                EmailSet = true;
            }
        }

        public string? Password
        {
            get => _password;
            set
            {
                _password = value;
                PasswordSet = true;
            }
        }

        public string? Bio
        {
            get => _bio;
            set
            {
                _bio = value;
                BioSet = true;
            }
        }

        public string? Image
        {
            get => _image;
            set
            {
                _image = value;
                ImageSet = true;
            }
        }

        [JsonIgnore]
        public bool UsernameSet { get; private set; }

        [JsonIgnore]
        public bool EmailSet { get; private set; }

        [JsonIgnore]
        public bool PasswordSet { get; private set; }

        [JsonIgnore]
        public bool BioSet { get; private set; }

        [JsonIgnore]
        public bool ImageSet { get; private set; }
    }

    public record Command(UserData User) : IRequest<UserEnvelope>;

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.User).NotNull();
            RuleFor(x => x.User.Username)
                .NotEmpty()
                .WithMessage(Constants.BLANK)
                .When(x => x.User.UsernameSet);
            RuleFor(x => x.User.Email)
                .NotEmpty()
                .WithMessage(Constants.BLANK)
                .When(x => x.User.EmailSet);
            RuleFor(x => x.User.Password)
                .NotEmpty()
                .WithMessage(Constants.BLANK)
                .MinimumLength(8)
                .WithMessage(Constants.PASSWORD_TOO_SHORT)
                .When(x => x.User.PasswordSet);
        }
    }

    public class Handler(
        ConduitContext context,
        IPasswordHasher passwordHasher,
        ICurrentUserAccessor currentUserAccessor,
        IJwtTokenGenerator jwtTokenGenerator,
        ConduitMapper mapper
    ) : IRequestHandler<Command, UserEnvelope>
    {
        public async ValueTask<UserEnvelope> Handle(
            Command message,
            CancellationToken cancellationToken
        )
        {
            var currentUsername = currentUserAccessor.GetCurrentUsername();
            var person = await context
                .Persons.Where(x => x.Username == currentUsername)
                .FirstOrDefaultAsync(cancellationToken);
            if (person is null)
            {
                throw new RestException(HttpStatusCode.NotFound, "user", Constants.NOT_FOUND);
            }

            if (message.User.UsernameSet && message.User.Username != person.Username)
            {
                if (
                    await context
                        .Persons.Where(x => x.Username == message.User.Username)
                        .AnyAsync(cancellationToken)
                )
                {
                    throw new RestException(HttpStatusCode.Conflict, "username", Constants.IN_USE);
                }

                person.Username = message.User.Username;
            }

            if (message.User.EmailSet && message.User.Email != person.Email)
            {
                if (
                    await context
                        .Persons.Where(x => x.Email == message.User.Email)
                        .AnyAsync(cancellationToken)
                )
                {
                    throw new RestException(HttpStatusCode.Conflict, "email", Constants.IN_USE);
                }

                person.Email = message.User.Email;
            }

            // empty strings on the nullable fields are normalized to null per the spec
            if (message.User.BioSet)
            {
                person.Bio = string.IsNullOrEmpty(message.User.Bio) ? null : message.User.Bio;
            }

            if (message.User.ImageSet)
            {
                person.Image = string.IsNullOrEmpty(message.User.Image) ? null : message.User.Image;
            }

            if (message.User.PasswordSet && !string.IsNullOrWhiteSpace(message.User.Password))
            {
                var salt = Guid.NewGuid().ToByteArray();
                person.Hash = await passwordHasher.Hash(message.User.Password, salt);
                person.Salt = salt;
            }

            await context.SaveChangesAsync(cancellationToken);

            var user = mapper.PersonToUser(person);
            user.Token = jwtTokenGenerator.CreateToken(
                person.Username ?? throw new InvalidOperationException()
            );
            return new UserEnvelope(user);
        }
    }
}

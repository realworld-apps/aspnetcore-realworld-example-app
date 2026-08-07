using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Features.Users;

[Route("users")]
public class UsersController(IMediator mediator)
{
    [HttpPost]
    public async ValueTask<ObjectResult> Create(
        [FromBody] Create.Command command,
        CancellationToken cancellationToken
    ) =>
        new(await mediator.Send(command, cancellationToken))
        {
            StatusCode = StatusCodes.Status201Created,
        };

    [HttpPost("login")]
    public ValueTask<UserEnvelope> Login(
        [FromBody] Login.Command command,
        CancellationToken cancellationToken
    ) => mediator.Send(command, cancellationToken);
}

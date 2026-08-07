using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Features.Users;

[Route("users")]
public class UsersController(IMediator mediator)
{
    [HttpPost]
    public async Task<ObjectResult> Create(
        [FromBody] Create.Command command,
        CancellationToken cancellationToken
    ) =>
        new(await mediator.Send(command, cancellationToken))
        {
            StatusCode = StatusCodes.Status201Created,
        };

    [HttpPost("login")]
    public Task<UserEnvelope> Login(
        [FromBody] Login.Command command,
        CancellationToken cancellationToken
    ) => mediator.Send(command, cancellationToken);
}

using System.Threading;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Security;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Features.Users;

[Route("user")]
[Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
public class UserController(IMediator mediator, ICurrentUserAccessor currentUserAccessor)
{
    [HttpGet]
    public ValueTask<UserEnvelope> GetCurrent(CancellationToken cancellationToken) =>
        mediator.Send(
            new Details.Query(currentUserAccessor.GetCurrentPersonId() ?? 0),
            cancellationToken
        );

    [HttpPut]
    public ValueTask<UserEnvelope> UpdateUser(
        [FromBody] Edit.Command command,
        CancellationToken cancellationToken
    ) => mediator.Send(command, cancellationToken);
}

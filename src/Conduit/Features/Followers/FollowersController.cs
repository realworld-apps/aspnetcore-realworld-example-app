using System.Threading;
using System.Threading.Tasks;
using Conduit.Features.Profiles;
using Conduit.Infrastructure.Security;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Features.Followers;

[Route("profiles")]
public class FollowersController(IMediator mediator) : Controller
{
    [HttpPost("{username}/follow")]
    [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
    public ValueTask<ProfileEnvelope> Follow(
        string username,
        CancellationToken cancellationToken
    ) => mediator.Send(new Add.Command(username), cancellationToken);

    [HttpDelete("{username}/follow")]
    [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
    public ValueTask<ProfileEnvelope> Unfollow(
        string username,
        CancellationToken cancellationToken
    ) => mediator.Send(new Delete.Command(username), cancellationToken);
}

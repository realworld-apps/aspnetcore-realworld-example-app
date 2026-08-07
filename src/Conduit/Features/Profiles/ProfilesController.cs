using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Features.Profiles;

[Route("profiles")]
public class ProfilesController(IMediator mediator) : Controller
{
    [HttpGet("{username}")]
    public ValueTask<ProfileEnvelope> Get(string username, CancellationToken cancellationToken) =>
        mediator.Send(new Details.Query(username), cancellationToken);
}

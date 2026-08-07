using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Features.Tags;

[Route("tags")]
public class TagsController(IMediator mediator) : Controller
{
    [HttpGet]
    public ValueTask<TagsEnvelope> Get(CancellationToken cancellationToken) =>
        mediator.Send(new List.Query(), cancellationToken);
}

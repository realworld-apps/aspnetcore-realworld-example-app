using System.Threading;
using System.Threading.Tasks;
using Conduit.Infrastructure.Security;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Features.Comments;

[Route("articles")]
public class CommentsController(IMediator mediator) : Controller
{
    [HttpPost("{slug}/comments")]
    [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
    public async ValueTask<IActionResult> Create(
        string slug,
        [FromBody] Create.Model model,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await mediator.Send(new Create.Command(model, slug), cancellationToken)
        );

    [HttpGet("{slug}/comments")]
    public ValueTask<CommentsEnvelope> Get(string slug, CancellationToken cancellationToken) =>
        mediator.Send(new List.Query(slug), cancellationToken);

    [HttpDelete("{slug}/comments/{id}")]
    [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
    public async ValueTask<IActionResult> Delete(
        string slug,
        int id,
        CancellationToken cancellationToken
    )
    {
        await mediator.Send(new Delete.Command(slug, id), cancellationToken);
        return NoContent();
    }
}

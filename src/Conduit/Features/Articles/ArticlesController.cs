using System.Threading;
using System.Threading.Tasks;
using Conduit.Infrastructure.Security;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Features.Articles;

[Route("articles")]
public class ArticlesController(IMediator mediator) : Controller
{
    [HttpGet]
    public ValueTask<ArticlesEnvelope> Get(
        [FromQuery] string? tag,
        [FromQuery] string? author,
        [FromQuery] string? favorited,
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        CancellationToken cancellationToken
    ) => mediator.Send(new List.Query(tag, author, favorited, limit, offset), cancellationToken);

    [HttpGet("feed")]
    [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
    public ValueTask<ArticlesEnvelope> GetFeed(
        [FromQuery] string? tag,
        [FromQuery] string? author,
        [FromQuery] string? favorited,
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        CancellationToken cancellationToken
    ) =>
        mediator.Send(
            new List.Query(tag, author, favorited, limit, offset) { IsFeed = true },
            cancellationToken
        );

    [HttpGet("{slug}")]
    public ValueTask<ArticleEnvelope> Get(string slug, CancellationToken cancellationToken) =>
        mediator.Send(new Details.Query(slug), cancellationToken);

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
    public async ValueTask<IActionResult> Create(
        [FromBody] Create.Command command,
        CancellationToken cancellationToken
    ) => StatusCode(StatusCodes.Status201Created, await mediator.Send(command, cancellationToken));

    [HttpPut("{slug}")]
    [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
    public ValueTask<ArticleEnvelope> Edit(
        string slug,
        [FromBody] Edit.Model model,
        CancellationToken cancellationToken
    ) => mediator.Send(new Edit.Command(model, slug), cancellationToken);

    [HttpDelete("{slug}")]
    [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
    public async ValueTask<IActionResult> Delete(string slug, CancellationToken cancellationToken)
    {
        await mediator.Send(new Delete.Command(slug), cancellationToken);
        return NoContent();
    }
}

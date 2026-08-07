using System.Threading;
using System.Threading.Tasks;
using Conduit.Features.Articles;
using Conduit.Infrastructure.Security;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Features.Favorites;

[Route("articles")]
public class FavoritesController(IMediator mediator) : Controller
{
    [HttpPost("{slug}/favorite")]
    [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
    public ValueTask<ArticleEnvelope> FavoriteAdd(
        string slug,
        CancellationToken cancellationToken
    ) => mediator.Send(new Add.Command(slug), cancellationToken);

    [HttpDelete("{slug}/favorite")]
    [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
    public ValueTask<ArticleEnvelope> FavoriteDelete(
        string slug,
        CancellationToken cancellationToken
    ) => mediator.Send(new Delete.Command(slug), cancellationToken);
}

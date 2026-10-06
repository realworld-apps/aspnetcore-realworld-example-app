using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Conduit.Infrastructure;

public class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public int? GetCurrentPersonId()
    {
        var subject = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        return
            subject is not null
            && subject.StartsWith("user:", System.StringComparison.Ordinal)
            && int.TryParse(
                subject[5..],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var id
            )
            && id > 0
            ? id
            : null;
    }
}

using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Conduit.Infrastructure;

/// <summary>
/// Roots every attribute-routed controller under a common prefix ("api" by default,
/// configurable through the ApiPrefix configuration key).
/// </summary>
public class ApiRoutePrefixConvention(string prefix) : IApplicationModelConvention
{
    private readonly AttributeRouteModel _prefix = new(new RouteAttribute(prefix));

    public void Apply(ApplicationModel application)
    {
        foreach (var selector in application.Controllers.SelectMany(c => c.Selectors))
        {
            selector.AttributeRouteModel =
                selector.AttributeRouteModel == null
                    ? _prefix
                    : AttributeRouteModel.CombineAttributeRouteModel(
                        _prefix,
                        selector.AttributeRouteModel
                    );
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Conduit.Infrastructure;

public class ValidatorActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!filterContext.ModelState.IsValid)
        {
            var result = new ContentResult();
            var errors = new Dictionary<string, string[]>();

            foreach (var valuePair in filterContext.ModelState)
            {
                errors.Add(valuePair.Key, [.. valuePair.Value.Errors.Select(x => x.ErrorMessage)]);
            }

            var content = JsonSerializer.Serialize(new { errors });
            result.Content = content;
            result.ContentType = "application/json";

            filterContext.HttpContext.Response.StatusCode = 422; //unprocessable entity;
            filterContext.Result = result;
        }
    }

    public void OnActionExecuted(ActionExecutedContext filterContext) { }
}

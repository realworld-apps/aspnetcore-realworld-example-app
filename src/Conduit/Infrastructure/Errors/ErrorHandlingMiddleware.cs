using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Conduit.Infrastructure.Errors;

public class ErrorHandlingMiddleware(
    RequestDelegate next,
    IStringLocalizer<ErrorHandlingMiddleware> localizer,
    ILogger<ErrorHandlingMiddleware> logger
)
{
    private static readonly Action<ILogger, string, Exception> LOGGER_MESSAGE =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            eventId: new EventId(id: 0, name: "ERROR"),
            formatString: "{Message}"
        );

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, logger, localizer);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        ILogger<ErrorHandlingMiddleware> logger,
        IStringLocalizer<ErrorHandlingMiddleware> localizer
    )
    {
        string? result;
        switch (exception)
        {
            case RestException re:
                context.Response.StatusCode = (int)re.Code;
                result = JsonSerializer.Serialize(new { errors = re.Errors });
                break;
            case FluentValidation.ValidationException ve:
                context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                result = JsonSerializer.Serialize(
                    new
                    {
                        errors = ve
                            .Errors.GroupBy(e => ToFieldName(e.PropertyName))
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).Distinct().ToArray()
                            ),
                    }
                );
                break;
            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                LOGGER_MESSAGE(logger, "Unhandled Exception", exception);
                result = JsonSerializer.Serialize(
                    new { errors = localizer[Constants.InternalServerError].Value }
                );
                break;
        }

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(result);
    }

    // "Model.Comment.Body" -> "body", "Article.TagList" -> "tagList"
    private static string ToFieldName(string propertyName)
    {
        var name = propertyName.Split('.')[^1];
        return name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
    }
}

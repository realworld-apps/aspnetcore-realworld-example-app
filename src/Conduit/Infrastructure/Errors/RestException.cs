using System;
using System.Collections.Generic;
using System.Net;

namespace Conduit.Infrastructure.Errors;

public class RestException(HttpStatusCode code, object? errors = null) : Exception
{
    public RestException(HttpStatusCode code, string field, string message)
        : this(code, new Dictionary<string, string[]> { [field] = [message] }) { }

    public object? Errors { get; } = errors;

    public HttpStatusCode Code { get; } = code;
}

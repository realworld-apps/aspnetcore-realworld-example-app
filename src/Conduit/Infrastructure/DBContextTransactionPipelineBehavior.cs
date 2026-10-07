using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Conduit.Infrastructure;

/// <summary>
/// Adds transaction to the processing pipeline
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class DBContextTransactionPipelineBehavior<TRequest, TResponse>(ConduitContext context)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        // Reads don't need a transaction spanning the entire handler.
        if (request is IReadOnlyRequest)
        {
            return await next(request, cancellationToken);
        }
        TResponse? result;

        try
        {
            await context.BeginTransactionAsync(cancellationToken);

            result = await next(request, cancellationToken);

            await context.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception)
        {
            await context.RollbackTransactionAsync();
            throw;
        }

        return result;
    }
}

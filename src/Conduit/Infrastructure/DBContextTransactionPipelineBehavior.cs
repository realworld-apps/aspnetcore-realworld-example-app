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
        TResponse? result;

        try
        {
            context.BeginTransaction();

            result = await next(request, cancellationToken);

            context.CommitTransaction();
        }
        catch (Exception)
        {
            context.RollbackTransaction();
            throw;
        }

        return result;
    }
}

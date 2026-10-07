using System;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using Mediator;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Conduit.IntegrationTests;

public class SliceFixture : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServiceProvider _provider;
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly IServiceScope _directScope;

    public SliceFixture()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConduit();

        _connection.Open();
        services.AddDbContext<ConduitContext>(options => options.UseSqlite(_connection));

        _provider = services.BuildServiceProvider();

        _directScope = _provider.CreateScope();
        GetDbContext().Database.EnsureCreated();
        _scopeFactory = _provider.GetRequiredService<IServiceScopeFactory>();
    }

    // A dedicated scope supports older tests that construct handlers directly.
    // Mediator and database helpers each use a fresh scoped context.
    public ConduitContext GetDbContext() =>
        _directScope.ServiceProvider.GetRequiredService<ConduitContext>();

    public void Dispose()
    {
        _directScope.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }

    public async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = _scopeFactory.CreateScope();
        await action(scope.ServiceProvider);
    }

    public async Task<T> ExecuteScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = _scopeFactory.CreateScope();
        return await action(scope.ServiceProvider);
    }

    public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request) =>
        ExecuteValueTaskScopeAsync(sp =>
        {
            var mediator = sp.GetRequiredService<IMediator>();

            return mediator.Send(request);
        });

    public ValueTask SendAsync(IRequest request) =>
        ExecuteValueTaskScopeAsync(async sp =>
        {
            var mediator = sp.GetRequiredService<IMediator>();

            await mediator.Send(request);
        });

    private async ValueTask<T> ExecuteValueTaskScopeAsync<T>(
        Func<IServiceProvider, ValueTask<T>> action
    )
    {
        using var scope = _scopeFactory.CreateScope();
        return await action(scope.ServiceProvider);
    }

    private async ValueTask ExecuteValueTaskScopeAsync(Func<IServiceProvider, ValueTask> action)
    {
        using var scope = _scopeFactory.CreateScope();
        await action(scope.ServiceProvider);
    }

    public Task ExecuteDbContextAsync(Func<ConduitContext, Task> action) =>
        ExecuteScopeAsync(sp => action(sp.GetRequiredService<ConduitContext>()));

    public Task<T> ExecuteDbContextAsync<T>(Func<ConduitContext, Task<T>> action) =>
        ExecuteScopeAsync(sp => action(sp.GetRequiredService<ConduitContext>()));

    public Task InsertAsync(params object[] entities) =>
        ExecuteDbContextAsync(db =>
        {
            foreach (var entity in entities)
            {
                db.Add(entity);
            }
            return db.SaveChangesAsync();
        });
}

using Conduit.Infrastructure;

namespace Conduit.IntegrationTests;

public class StubCurrentUserAccessor(string? userName) : ICurrentUserAccessor
{
    public string? GetCurrentUsername() => userName;
}

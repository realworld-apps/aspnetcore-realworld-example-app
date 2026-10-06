using Conduit.Infrastructure;

namespace Conduit.IntegrationTests;

public class StubCurrentUserAccessor(string? userName, int personId = 1) : ICurrentUserAccessor
{
    public int? GetCurrentPersonId() => userName is null ? null : personId;
}

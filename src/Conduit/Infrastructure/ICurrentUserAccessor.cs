namespace Conduit.Infrastructure;

public interface ICurrentUserAccessor
{
    public int? GetCurrentPersonId();
}

namespace Conduit.Infrastructure;

public interface ICurrentUserAccessor
{
    public string? GetCurrentUsername();
}

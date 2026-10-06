namespace Conduit.Infrastructure.Security;

public interface IJwtTokenGenerator
{
    public string CreateToken(int personId);
}

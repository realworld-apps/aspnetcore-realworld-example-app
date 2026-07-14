namespace Conduit.Infrastructure.Security;

public interface IJwtTokenGenerator
{
    public string CreateToken(string username);
}

using Conduit.Infrastructure.Security;

namespace Conduit.IntegrationTests;

public class StubJwtTokenGenerator : IJwtTokenGenerator
{
    public string CreateToken(string username) => "stub-token";
}

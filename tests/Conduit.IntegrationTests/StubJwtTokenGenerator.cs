using Conduit.Infrastructure.Security;

namespace Conduit.IntegrationTests;

public class StubJwtTokenGenerator : IJwtTokenGenerator
{
    public string CreateToken(int personId) => "stub-token";
}

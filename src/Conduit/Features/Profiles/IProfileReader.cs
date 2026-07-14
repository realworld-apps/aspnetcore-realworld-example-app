using System.Threading;
using System.Threading.Tasks;

namespace Conduit.Features.Profiles;

public interface IProfileReader
{
    public Task<ProfileEnvelope> ReadProfile(string username, CancellationToken cancellationToken);
}

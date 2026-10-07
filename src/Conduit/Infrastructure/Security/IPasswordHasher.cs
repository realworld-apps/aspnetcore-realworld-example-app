using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Conduit.Infrastructure.Security;

public interface IPasswordHasher : IDisposable
{
    public Task<byte[]> Hash(string password, byte[] salt);
    public PasswordVerificationResult Verify(string password, byte[] hash, byte[] salt);
}

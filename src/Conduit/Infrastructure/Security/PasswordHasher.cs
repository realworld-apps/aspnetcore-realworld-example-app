using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Conduit.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    // Identity's versioned payload carries its own random salt and work factor.
    private readonly PasswordHasher<object> _hasher = new(
        Options.Create(new PasswordHasherOptions { IterationCount = 210_000 })
    );

    public Task<byte[]> Hash(string password, byte[] salt)
    {
        return Task.FromResult(Convert.FromBase64String(_hasher.HashPassword(this, password)));
    }

    public PasswordVerificationResult Verify(string password, byte[] hash, byte[] salt)
    {
        // Legacy HMAC hashes are exactly 64 bytes. They are verified only, never issued.
        if (hash.Length == 64 && salt.Length == 16)
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var input = new byte[bytes.Length + salt.Length];
            bytes.CopyTo(input, 0);
            salt.CopyTo(input, bytes.Length);
            var expected = HMACSHA512.HashData(Encoding.UTF8.GetBytes("realworld"), input);
            return CryptographicOperations.FixedTimeEquals(hash, expected)
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Failed;
        }

        return hash.Length == 0
            ? PasswordVerificationResult.Failed
            : _hasher.VerifyHashedPassword(this, Convert.ToBase64String(hash), password);
    }

    public void Dispose() { }
}

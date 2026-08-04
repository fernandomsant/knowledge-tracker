using System.Security.Cryptography;
using KnowledgeTracker.Application.Authentication;

namespace KnowledgeTracker.Infrastructure.Authentication;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 600000;

    public string Hash(string value)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(value, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return Iterations + ":" + Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    public bool Verify(string value, string encoded)
    {
        try
        {
            var parts = encoded.Split(':');
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(value, Convert.FromBase64String(parts[1]), int.Parse(parts[0]), HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}
using System.Security.Cryptography;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Infrastructure.Authentication;

public sealed class OpaqueRefreshTokenService(byte[] pepper) : IRefreshTokenService
{
    public RefreshToken Create() => new(Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));

    public RefreshTokenHash Hash(RefreshToken token)
    {
        var raw = Convert.FromBase64String(token.Value);
        return new RefreshTokenHash(HMACSHA512.HashData(pepper, raw));
    }

    public RefreshTokenHash? TryHash(RefreshToken token)
    {
        try
        {
            return Hash(token);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Infrastructure.Authentication;

public sealed class HmacAccessTokenService(byte[] key) : IAccessTokenService
{
    public AccessToken Issue(AccessToken unsignedToken)
    {
        var header = Encode(Encoding.UTF8.GetBytes("{\"alg\":\"HS512\",\"typ\":\"at+jwt\"}"));
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                iss = unsignedToken.Claims.Issuer,
                sub = unsignedToken.Claims.Subject.ToString(),
                aud = unsignedToken.Audience,
                auth_time = unsignedToken.Claims.AuthenticatedAtUtc.ToUnixTimeSeconds(),
                exp = unsignedToken.Claims.ExpiresAtUtc.ToUnixTimeSeconds(),
                nonce = unsignedToken.Claims.Nonce.ToString(),
                sid = unsignedToken.SessionId.ToString(),
                token_use = "access",
            }
        );
        var signed = header + "." + Encode(payload);
        var value = signed + "." + Encode(HMACSHA512.HashData(key, Encoding.ASCII.GetBytes(signed)));
        return unsignedToken.WithValue(value);
    }

    private static string Encode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
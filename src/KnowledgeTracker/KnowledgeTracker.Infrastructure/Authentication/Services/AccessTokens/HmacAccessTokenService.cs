using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Infrastructure.Authentication;

public sealed class HmacAccessTokenService(byte[] key, AuthenticationOptions options)
    : IAccessTokenService
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

    public AccessToken? Validate(AccessTokenReference token)
    {
        try
        {
            var segments = token.Value.Split('.');
            if (segments.Length != 3)
                return null;

            var signed = segments[0] + "." + segments[1];
            var expectedSignature = HMACSHA512.HashData(key, Encoding.ASCII.GetBytes(signed));
            if (!CryptographicOperations.FixedTimeEquals(expectedSignature, Decode(segments[2])))
                return null;

            using var payload = JsonDocument.Parse(Decode(segments[1]));
            var root = payload.RootElement;
            if (
                !root.TryGetProperty("iss", out var issuer)
                || issuer.GetString() != options.Issuer
                || !root.TryGetProperty("aud", out var audience)
                || audience.GetString() != options.Audience
                || !root.TryGetProperty("token_use", out var tokenUse)
                || tokenUse.GetString() != "access"
                || !Guid.TryParse(root.GetProperty("sub").GetString(), out var subject)
                || !Guid.TryParse(root.GetProperty("nonce").GetString(), out var nonce)
                || !Guid.TryParse(root.GetProperty("sid").GetString(), out var sessionId)
            )
                return null;

            var authenticatedAt = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("auth_time").GetInt64());
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64());
            if (expiresAt <= DateTimeOffset.UtcNow)
                return null;

            return new AccessToken(
                new TokenClaims(options.Issuer, subject, authenticatedAt, expiresAt, nonce),
                sessionId,
                options.Audience,
                token.Value
            );
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Encode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Decode(string value)
    {
        var padding = value.Length % 4;
        return Convert.FromBase64String(
            value.Replace('-', '+').Replace('_', '/') + (padding == 0 ? "" : new string('=', 4 - padding))
        );
    }
}
using System.Security.Cryptography;
using KnowledgeTracker.Application.Authentication;
namespace KnowledgeTracker.Infrastructure.Authentication;
public sealed class Pbkdf2PasswordHasher:IPasswordHasher
{const int Iterations=600000;public string Hash(string value){var salt=RandomNumberGenerator.GetBytes(16);var hash=Rfc2898DeriveBytes.Pbkdf2(value,salt,Iterations,HashAlgorithmName.SHA256,32);return Iterations.ToString()+':' +Convert.ToBase64String(salt)+':' +Convert.ToBase64String(hash);}public bool Verify(string value,string encoded){try{var p=encoded.Split(':');var expected=Convert.FromBase64String(p[2]);var actual=Rfc2898DeriveBytes.Pbkdf2(value,Convert.FromBase64String(p[1]),int.Parse(p[0]),HashAlgorithmName.SHA256,expected.Length);return CryptographicOperations.FixedTimeEquals(actual,expected);}catch{return false;}}}
public sealed class OpaqueRefreshTokenService(byte[] pepper):IRefreshTokenService
{public RefreshTokenMaterial Create(){var raw=RandomNumberGenerator.GetBytes(64);return new(Convert.ToBase64String(raw),Hash(raw));}public byte[] Hash(string value){try{return Hash(Convert.FromBase64String(value));}catch{return Array.Empty<byte>();}}byte[] Hash(byte[] raw)=>HMACSHA512.HashData(pepper,raw);}

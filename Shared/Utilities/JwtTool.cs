using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Utilities;

public class JwtTool
{
     private const string AppleJwksUrl = "https://appleid.apple.com/auth/keys";

    public static async Task<ClaimsPrincipal?> ValidateAppleJwtAsync(string jwt, string clientId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.ReadJwtToken(jwt);
        foreach (var claim in token.Claims) Console.WriteLine($"Decoded Claim: {claim.Type} = {claim.Value}");

        var jwks = await GetAppleJwksAsync();
        if (jwks is null)
            return null;

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new List<string> { "https://appleid.apple.com" },
            ValidateAudience = true,
            ValidAudiences = new List<string> { clientId },
            ValidateLifetime = true,
            IssuerSigningKeys = jwks
        };
        try
        {
            var principal = tokenHandler.ValidateToken(jwt, tokenValidationParameters, out var validatedToken);
            var jwtToken = validatedToken as JwtSecurityToken;
            if (jwtToken != null)
            {
                var identity = new ClaimsIdentity(principal.Identity);
                foreach (var claim in jwtToken.Claims)
                    if (!identity.HasClaim(c => c.Type == claim.Type))
                        identity.AddClaim(claim);
                return new ClaimsPrincipal(identity);
            }

            return principal;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<IEnumerable<SecurityKey>?> GetAppleJwksAsync()
    {
        using var client = new HttpClient();
        var jwks = await client.GetFromJsonAsync<AppleJwks>(AppleJwksUrl)!;

        foreach (var key in jwks.Keys)
            try
            {
                var modulusBytes = Convert.FromBase64String(key.N);
                var exponentBytes = Convert.FromBase64String(key.E);

                Console.WriteLine(
                    $"Key {key.Kid}: Modulus length = {modulusBytes.Length}, Exponent length = {exponentBytes.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid Base64 in key {key.Kid}: {ex.Message}");
            }

        return jwks?.Keys?.Select(k =>
        {
            try
            {
                var modulusBytes = Convert.FromBase64String(Base64UrlToBase64(k.N));
                var exponentBytes = Convert.FromBase64String(Base64UrlToBase64(k.E));

                return new RsaSecurityKey(new RSAParameters
                {
                    Modulus = modulusBytes,
                    Exponent = exponentBytes
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing key {k.Kid}: {ex.Message}");
                return null;
            }
        }).Where(key => key != null)!;
    }

    private static string Base64UrlToBase64(string base64Url)
    {
        var padded = base64Url.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return padded;
    }
}

public class AppleJwks
{
    public List<AppleJwk> Keys { get; set; } = new();
}

public class AppleJwk
{
    public string Kty { get; set; } = string.Empty;
    public string Kid { get; set; } = string.Empty;
    public string Use { get; set; } = string.Empty;
    public string Alg { get; set; } = string.Empty;
    public string N { get; set; } = string.Empty;
    public string E { get; set; } = string.Empty;
}
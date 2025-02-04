using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shared.Models;

namespace Shared.Utilities;

/// <summary>
/// A utility class for handling JSON Web Tokens (JWT). This class provides methods for generating and validating JWTs
/// that can be used for authentication and authorization purposes in the application.
/// </summary>
public class JwtTool
{
    private readonly IConfiguration _configuration;

    public JwtTool(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Retrieves the ClaimsPrincipal from an expired JWT token.
    /// This method validates the token even if it is expired, depending on the parameter provided.
    /// </summary>
    /// <param name="token">The expired JWT token to be validated and parsed.</param>
    /// <param name="validateLifetime">Indicates whether the token's lifetime should be validated. Default is true.</param>
    /// <returns>
    /// A ClaimsPrincipal object extracted from the token if validation succeeds; otherwise, null.
    /// </returns>
    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token, bool validateLifetime = true)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(_configuration["Jwt:Secret"])),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            ValidateLifetime = validateLifetime // can allow expired tokens
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
    
    
    /// <summary>
    ///     Create a JWT valid for usage on the site.
    ///     This is the Vison Jam JWT and can be obtained by any type of signin method
    ///     Apple, Google or Vision Hame.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public string GenerateAppJwt(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.GivenName, user.NameComponents.GivenName ?? ""),
            new(ClaimTypes.Surname, user.NameComponents.FamilyName ?? "")
        };

        var key = new SymmetricSecurityKey(Convert.FromBase64String(_configuration["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    
    private const string AppleJwksUrl = "https://appleid.apple.com/auth/keys";

    /// <summary>
    /// Validates an Apple JSON Web Token (JWT) and returns the associated claims if the token is valid.
    /// </summary>
    /// <param name="jwt">The JWT string to validate.</param>
    /// <param name="validateLifetime">A boolean indicating whether to validate the token's lifetime. Default is true.</param>
    /// <returns>
    /// A <see cref="ClaimsPrincipal"/> containing the claims from the validated token if valid; otherwise, null.
    /// </returns>
    public async Task<ClaimsPrincipal?> ValidateAppleJwtAsync(string jwt, bool validateLifetime = true)
    {
        var clientId = _configuration["Apple:ClientId"];
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentNullException(nameof(clientId));
        
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

    /// <summary>
    /// Retrieves the JSON Web Key Set (JWKS) from Apple's authentication server and returns a collection of security keys.
    /// </summary>
    /// <returns>
    /// A collection of <see cref="SecurityKey"/> objects parsed from Apple's JWKS if successful; otherwise, null.
    /// </returns>
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

    /// <summary>
    /// Converts a Base64Url-encoded string to a standard Base64-encoded string.
    /// </summary>
    /// <param name="base64Url">The Base64Url-encoded string to convert.</param>
    /// <returns>
    /// A standard Base64-encoded string equivalent to the provided Base64Url-encoded string.
    /// </returns>
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
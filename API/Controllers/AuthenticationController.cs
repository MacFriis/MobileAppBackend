using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Middleware;
using Shared.Models;
using Shared.Models.Responses;
using Shared.Utilities;

namespace API.Controllers;

/// <summary>
/// The AuthenticationController handles user authentication operations, including login and account validation.
/// </summary>
/// <remarks>
/// This controller is designed with API versioning and requires a valid API key for accessing its endpoints.
/// </remarks>
[ApiController]
[Route("v{version:apiVersion}[controller]")]
[ApiVersion(1.0)]
[ApiKey]
public class AuthenticationController: ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationController> _logger;
    private readonly JwtTool _jwtTool;


    /// <summary>
    /// Controller responsible for handling user authentication operations, such as login and account validation.
    /// </summary>
    /// <remarks>
    /// This controller is decorated with API versioning and requires an API key for access.
    /// </remarks>
    public AuthenticationController(
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        ILogger<AuthenticationController> logger
            )
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
        _jwtTool = new JwtTool(configuration);
    }

    #region Sign in With Apple
    /// <summary>
    ///     Signin or signup using "Sign in With Apple"
    ///     Any user is welcome, so no rejecting unless the validation of the SIWA jwt is not valid
    /// </summary>
    /// <param name="Authorization"></param>
    /// <returns></returns>
    [HttpPost("signinwithapple")]
    public async Task<IActionResult> SigninWIthAppe([FromHeader] string Authorization)
    {
        _logger.LogInformation("Signin with Apple");
        try
        {
            if (string.IsNullOrEmpty(Authorization) || !Authorization.StartsWith("Bearer "))
                return Unauthorized(new { Message = "Missing or invalid Authorization header" });


            var appleJwt = Authorization.Substring("Bearer ".Length);
            var appleClaims = await _jwtTool.ValidateAppleJwtAsync(appleJwt);
            if (appleClaims is null)
                return Unauthorized(new { Message = "Invalid Apple JWT" });
            
            var appleUserId = appleClaims.FindFirstValue("sub");
            var email = appleClaims.FindFirstValue("email");

            if (appleUserId is null || email is null)
                return Unauthorized(new { Message = "Invalid Apple JWT" });

            var user = await _userManager.FindByIdAsync(appleUserId);
            if (user is null)
            {
                user = new AppUser
                {
                    Id = appleUserId,
                    UserName = appleUserId,
                    Email = email,
                    NameComponents = new PersonNameComponents
                    {
                        GivenName = appleClaims.FindFirstValue("given_name"),
                        FamilyName = appleClaims.FindFirstValue("family_name")
                    }
                };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                    return BadRequest(new { Message = "Unable to create user", createResult.Errors });
            }

            var accessToken = _jwtTool.GenerateAppJwt(user);
            var refreshToken = Guid.NewGuid().ToString();
            var refreshTokenExpiryTime = DateTimeOffset.Now.AddMonths(1);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = refreshTokenExpiryTime;
            await _userManager.UpdateAsync(user);

            return Ok(new AuthenticationResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expires = refreshTokenExpiryTime
            });
        } catch (Exception e)
        {
            _logger.LogError(e, "Unable to validate Apple JWT");
            return BadRequest(new { Message = "Unable to validate Apple JWT", Error = e.Message });
        }   
    }
    #endregion
    
    
    
}
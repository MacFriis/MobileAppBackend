using System.Reflection;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// The SystemController class provides endpoints for retrieving application build/version information
/// and serving files required for specific platform functionalities.
/// </summary>
[ApiController, Route("[controller]")]
public class SystemController: ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemController> _logger;

    public SystemController(
        IConfiguration configuration,
        ILogger<SystemController> logger
        )
    {
        _configuration = configuration;
        _logger = logger;
    }


    /// <summary>
    /// Provides build and version information about the application, including build metadata.
    /// </summary>
    /// <returns>Returns an action result containing the application version, file version, and build information in JSON format.</returns>
    [HttpGet]
    public async Task<ActionResult> About()
    {
        var json = await System.IO.File.ReadAllTextAsync("build-info.json");
        dynamic? buildInfo = JsonSerializer.Deserialize<dynamic>(json);
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyname = assembly.GetName();
        var version = assemblyname.Version;
        var assemblyVersion = $"{version.Major}.{version.Minor}.{version.Build}";
        var assemblyFileVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        return Ok(new
        {
            Version = assemblyVersion,
            FileVersion = assemblyFileVersion,
            BuildInfo = buildInfo
        });
    }

    /// <summary>
    /// Retrieves the Apple App Site Association file content required for Universal Links functionality in iOS applications.
    /// </summary>
    /// <returns>Returns a JSON content of the Apple App Site Association file.</returns>
    [HttpGet(".well-known/apple-app-site-association")]
    public async Task<ActionResult> AppleAppSiteAssociation()
    {
        var appleAppSiteAssociation = await System.IO.File.ReadAllTextAsync("apple-app-site-association.json");
        return Ok(appleAppSiteAssociation);
    }
}
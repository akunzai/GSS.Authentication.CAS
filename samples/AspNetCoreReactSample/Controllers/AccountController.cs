using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AspNetCoreReactSample.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly AuthenticationOptions _options;

    public AccountController(IOptions<AuthenticationOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet("auth-schemes")]
    public IEnumerable<string> GetAuthenticationSchemes()
    {
        return _options.Schemes.Where(x => !string.IsNullOrEmpty(x.DisplayName))
            .Select(x => x.Name);
    }

    [HttpGet("profile")]
    public UserProfile? GetUserProfile()
    {
        if (User.Identity?.IsAuthenticated != true) return null;
        return new UserProfile
        {
            Id =
                User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? User.Identity!.Name!,
            Name = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value,
            Email = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value
        };
    }

    [HttpGet("/account/login")]
    public IActionResult Login(string? scheme)
    {
        return string.IsNullOrWhiteSpace(scheme)
            ? Challenge(new AuthenticationProperties { RedirectUri = "/" })
            : Challenge(new AuthenticationProperties { RedirectUri = "/" }, scheme);
    }

    [HttpGet("/account/logout")]
    public async Task Logout(string? redirectUrl)
    {
        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            redirectUrl = "/";
        }

        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        await HttpContext.SignOutAsync(properties);
        var authScheme = User.Claims.FirstOrDefault(x => string.Equals(x.Type, "auth_scheme"))?.Value;
        if (!string.IsNullOrWhiteSpace(authScheme))
        {
            await HttpContext.SignOutAsync(authScheme, properties);
        }
    }
}

public class UserProfile
{
    public string Id { get; set; } = default!;
    public string? Name { get; set; }
    public string? Email { get; set; }
}
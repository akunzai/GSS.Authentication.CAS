using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AspNetCoreReactSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly AuthenticationOptions _options;

    public AccountController(IOptions<AuthenticationOptions> options)
    {
        _options = options.Value;
    }

    [AllowAnonymous]
    [HttpGet("auth-schemes")]
    public IEnumerable<string> GetAuthenticationSchemes()
    {
        return _options.Schemes.Where(x => !string.IsNullOrEmpty(x.DisplayName))
            .Select(x => x.Name);
    }

    [HttpGet("profile")]
    public UserProfile GetUserProfile()
    {
        return new UserProfile
        {
            Id =
                User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? User.Identity!.Name!,
            Name = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value ??
                   User.Claims.FirstOrDefault(x => x.Type == "display_name")?.Value,
            Email = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value
        };
    }

    [AllowAnonymous]
    [HttpGet("/account/login")]
    public IActionResult Login(string? scheme)
    {
        return string.IsNullOrWhiteSpace(scheme)
            ? Challenge(new AuthenticationProperties { RedirectUri = "/" })
            : Challenge(new AuthenticationProperties { RedirectUri = "/" }, scheme);
    }

    [AllowAnonymous]
    [HttpGet("/account/logout")]
    public IActionResult Logout()
    {
        return SignOut();
    }
}

public class UserProfile
{
    public string Id { get; set; } = default!;
    public string? Name { get; set; }
    public string? Email { get; set; }
}
using GSS.Authentication.CAS.E2E.Tests.Fixtures;

namespace GSS.Authentication.CAS.E2E.Tests.Tests;

/// <summary>
/// E2E tests for AspNetCoreSample.
/// These tests require the sample application to be running with Keycloak.
/// </summary>
public class AspNetCoreSampleTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _playwright;
    private const string BaseUrl = "https://localhost:5001";

    public AspNetCoreSampleTests(PlaywrightFixture playwright)
    {
        _playwright = playwright;
    }

    [Fact]
    public async Task HomePage_ShowsAnonymousMessage_WhenNotAuthenticated()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Hello, anonymous");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Login" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task LoginPage_ShowsAuthenticationSchemes()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{BaseUrl}/Account/Login");

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Choose an authentication scheme");
    }
}

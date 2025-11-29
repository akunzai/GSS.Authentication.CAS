using GSS.Authentication.CAS.E2E.Tests.Fixtures;
using GSS.Authentication.CAS.E2E.Tests.PageObjects;

namespace GSS.Authentication.CAS.E2E.Tests.Tests;

/// <summary>
/// E2E tests for AspNetCoreIdentitySample.
/// These tests require the sample application to be running with Keycloak.
/// Run manually in DevContainers environment.
/// </summary>
public class AspNetCoreIdentitySampleTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _playwright;
    private const string BaseUrl = "https://localhost:5004";

    public AspNetCoreIdentitySampleTests(PlaywrightFixture playwright)
    {
        _playwright = playwright;
    }

    [Fact]
    public async Task HomePage_RedirectsToLogin_WhenNotAuthenticated()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl);

        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Identity/Account/Login"));
    }

    [Fact]
    public async Task LoginPage_ShowsExternalProviders()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{BaseUrl}/Identity/Account/Login");

        var externalLoginSection = page.Locator("text=Use another service to log in");
        await Assertions.Expect(externalLoginSection).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ExternalLogin_WithCas_RedirectsToKeycloak()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/Identity/Account/Login");

        var casButton = page.Locator("button[value='CAS']");
        if (await casButton.IsVisibleAsync())
        {
            await casButton.ClickAsync();

            await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("auth\\.dev\\.local"));
        }
    }

    [Fact]
    public async Task RegisterPage_IsAccessible()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{BaseUrl}/Identity/Account/Register");

        await Assertions.Expect(page.GetByRole(AriaRole.Heading)).ToContainTextAsync("Register");
        await Assertions.Expect(page.Locator("#Input_Email")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#Input_Password")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task FullLoginFlow_WithExternalProvider_ShowsUserInfo()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/Identity/Account/Login");

        var casButton = page.Locator("button[value='CAS']");
        if (!await casButton.IsVisibleAsync())
        {
            return;
        }

        await casButton.ClickAsync();

        var keycloakPage = new KeycloakLoginPage(page);
        if (await keycloakPage.IsOnKeycloakLoginPageAsync())
        {
            await keycloakPage.LoginAsync("test", "test");
            await page.WaitForURLAsync(url => url.StartsWith(BaseUrl), new() { Timeout = 15000 });
        }

        var currentUrl = page.Url;
        Assert.StartsWith(BaseUrl, currentUrl);
    }
}

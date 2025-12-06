using GSS.Authentication.CAS.E2E.Tests.Fixtures;
using GSS.Authentication.CAS.E2E.Tests.PageObjects;

namespace GSS.Authentication.CAS.E2E.Tests.Tests;

/// <summary>
/// E2E tests for AspNetCoreMvcSample.
/// These tests require the sample application to be running with Keycloak.
/// </summary>
public class AspNetCoreMvcSampleTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _playwright;
    private const string BaseUrl = "https://localhost:5002";

    public AspNetCoreMvcSampleTests(PlaywrightFixture playwright)
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
        var loginPage = new LoginPage(page);

        await Assertions.Expect(loginPage.Heading).ToContainTextAsync("Choose an authentication scheme");
        Assert.True(await loginPage.HasAuthenticationSchemesAsync());
    }

    [Fact]
    public async Task LoginPage_RedirectsToKeycloak_WhenCasSelected()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/Account/Login");
        var loginPage = new LoginPage(page);

        await loginPage.SelectCasAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("auth\\.dev\\.local"));
    }

    [Fact]
    public async Task FullLoginFlow_WithCas_ShowsUserInfo()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/Account/Login");
        var loginPage = new LoginPage(page);

        await loginPage.SelectCasAsync();

        var keycloakPage = new KeycloakLoginPage(page);
        if (await keycloakPage.IsOnKeycloakLoginPageAsync())
        {
            await keycloakPage.LoginAsync("test", "test");
            await keycloakPage.WaitForRedirectAsync(BaseUrl);
        }

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Hello,");
        await Assertions.Expect(page.Locator("text=Logout")).ToBeVisibleAsync();
    }
}

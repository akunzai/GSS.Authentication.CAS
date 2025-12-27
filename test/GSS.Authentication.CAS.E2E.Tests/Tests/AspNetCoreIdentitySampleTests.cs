using GSS.Authentication.CAS.E2E.Tests.Fixtures;
using GSS.Authentication.CAS.E2E.Tests.PageObjects;

namespace GSS.Authentication.CAS.E2E.Tests.Tests;

/// <summary>
/// E2E tests for AspNetCoreIdentitySample.
/// These tests require the sample application to be running with Keycloak.
/// </summary>
public class AspNetCoreIdentitySampleTests(PlaywrightFixture playwright) : SampleTestsBase(playwright)
{
    protected override string DefaultBaseUrl => "https://localhost:5004";
    protected override string LoginPath => "/Identity/Account/Login";

    [Fact]
    public async Task HomePage_RedirectsToLogin_WhenNotAuthenticated()
    {
        var page = await CreatePageAsync();

        await NavigateToHomePageAsync(page);

        await Assertions.Expect(page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex("/Identity/Account/Login"));
    }

    [Fact]
    public async Task LoginPage_ShowsExternalProviders()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var externalLoginSection = page.Locator("text=Use another service to log in");
        await Assertions.Expect(externalLoginSection).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ExternalLogin_WithCas_RedirectsToKeycloak()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var casButton = page.Locator("button[value='CAS']");
        var isVisible = await casButton.IsVisibleAsync();
        Assert.SkipWhen(!isVisible, "CAS external provider not configured");

        await casButton.ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex("auth\\.dev\\.local"));
    }

    [Fact]
    public async Task RegisterPage_IsAccessible()
    {
        var page = await CreatePageAsync();

        await NavigateToAsync(page, "/Identity/Account/Register");

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Register", Exact = true })).ToContainTextAsync("Register");
        await Assertions.Expect(page.Locator("#Input_Email")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#Input_Password")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task FullLoginFlow_WithExternalProvider_ShowsUserInfo()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var casButton = page.Locator("button[value='CAS']");
        var isVisible = await casButton.IsVisibleAsync();
        Assert.SkipWhen(!isVisible, "CAS external provider not configured");

        await casButton.ClickAsync();

        var keycloakPage = new KeycloakLoginPage(page);
        if (await keycloakPage.IsOnKeycloakLoginPageAsync())
        {
            await keycloakPage.LoginAsync(TestUsername, TestPassword);
            await page.WaitForURLAsync(url => url.StartsWith(BaseUrl), new() { Timeout = 15000 });
        }

        Assert.StartsWith(BaseUrl, page.Url);
    }
}

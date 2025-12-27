using GSS.Authentication.CAS.E2E.Tests.Fixtures;
using GSS.Authentication.CAS.E2E.Tests.PageObjects;

namespace GSS.Authentication.CAS.E2E.Tests.Tests;

/// <summary>
/// E2E tests for AspNetCoreSample.
/// These tests require the sample application to be running with Keycloak.
/// </summary>
public class AspNetCoreSampleTests(PlaywrightFixture playwright) : SampleTestsBase(playwright)
{
    protected override string DefaultBaseUrl => "https://localhost:5001";

    [Fact]
    public async Task HomePage_ShowsAnonymousMessage_WhenNotAuthenticated()
    {
        var page = await CreatePageAsync();
        await VerifyHomePageAnonymouslyAsync(page);
    }

    [Fact]
    public async Task LoginPage_ShowsAuthenticationSchemes()
    {
        var page = await CreatePageAsync();
        await VerifyLoginPageSchemesAsync(page);
    }

    [Fact]
    public async Task LoginPage_RedirectsToKeycloak_WhenCasSelected()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);
        var loginPage = new LoginPage(page);

        await loginPage.SelectCasAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("auth\\.dev\\.local"));
    }

    [Fact]
    public async Task FullLoginFlow_WithCas_ShowsUserInfo()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var loggedIn = await LoginWithCasAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Hello,");
        await Assertions.Expect(page.Locator("text=Logout")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AuthenticatedUser_CanAccessHomePage()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var loggedIn = await LoginWithCasAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await NavigateToHomePageAsync(page);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Hello,");
        await Assertions.Expect(page.Locator("text=Logout")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AuthenticatedUser_CanLogout()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var loggedIn = await LoginWithCasAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await NavigateToHomePageAsync(page);

        var logoutLink = page.GetByRole(AriaRole.Link, new() { Name = "Logout" });
        await logoutLink.ClickAsync();

        // Wait for page to reload after logout
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Hello, anonymous");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Login" }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task AuthenticatedUser_StaysLoggedIn_AfterNavigation()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var loggedIn = await LoginWithCasAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await NavigateToHomePageAsync(page);
        var username = await page.GetByRole(AriaRole.Heading, new() { Level = 1 }).TextContentAsync();

        // Navigate back to home page instead of login page
        // because logged-in users should stay on home page
        await NavigateToHomePageAsync(page);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync(username ?? string.Empty);
    }
}

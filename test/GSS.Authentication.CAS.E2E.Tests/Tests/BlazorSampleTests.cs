using GSS.Authentication.CAS.E2E.Tests.Fixtures;
using GSS.Authentication.CAS.E2E.Tests.PageObjects;

namespace GSS.Authentication.CAS.E2E.Tests.Tests;

/// <summary>
/// E2E tests for BlazorSample.
/// These tests require the sample application to be running with Keycloak.
/// </summary>
public class BlazorSampleTests(PlaywrightFixture playwright) : SampleTestsBase(playwright)
{
    protected override string DefaultBaseUrl => "https://localhost:5003";

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

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Hello,");
        await Assertions.Expect(page.Locator("text=Logout")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AuthenticatedUser_CanSeeClaims()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var loggedIn = await LoginWithCasAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var claimsDetails = page.Locator("details:has(summary:text('Claims'))");
        await Assertions.Expect(claimsDetails).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AuthenticatedUser_CanAccessHomePage()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var loggedIn = await LoginWithCasAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await NavigateToHomePageAsync(page);

        var logoutLink = page.GetByRole(AriaRole.Link, new() { Name = "Logout" });
        await logoutLink.ClickAsync();

        // Wait for page to reload after logout
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });

        // Blazor app redirects to login page after logout
        // Verify we're on login page or anonymous home page
        var heading = page.GetByRole(AriaRole.Heading, new() { Level = 1 });
        var headingText = await heading.TextContentAsync();

        // Either "Choose an authentication scheme" (login page) or "Hello, anonymous" (home page)
        Assert.True(
            headingText?.Contains("Choose an authentication scheme") == true ||
            headingText?.Contains("Hello, anonymous") == true,
            $"Expected login page or anonymous home page, but got: {headingText}");

        // Verify Login link/button is visible (user is logged out)
        var loginVisible = await page.GetByRole(AriaRole.Link, new() { Name = "Login" }).IsVisibleAsync() ||
                          await page.Locator("text=CAS").IsVisibleAsync();
        Assert.True(loginVisible, "Login option should be visible after logout");
    }

    [Fact]
    public async Task AuthenticatedUser_StaysLoggedIn_AfterNavigation()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var loggedIn = await LoginWithCasAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await NavigateToHomePageAsync(page);
        var username = await page.GetByRole(AriaRole.Heading, new() { Level = 1 }).TextContentAsync();

        // Navigate back to home page to verify session persistence
        await NavigateToHomePageAsync(page);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync(username ?? string.Empty);
    }
}

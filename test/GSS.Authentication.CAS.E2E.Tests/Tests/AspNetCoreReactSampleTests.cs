using GSS.Authentication.CAS.E2E.Tests.Fixtures;
using GSS.Authentication.CAS.E2E.Tests.PageObjects;

namespace GSS.Authentication.CAS.E2E.Tests.Tests;

/// <summary>
/// E2E tests for AspNetCoreReactSample.
/// These tests require the sample application to be running with Keycloak.
/// </summary>
public class AspNetCoreReactSampleTests(PlaywrightFixture playwright) : SampleTestsBase(playwright)
{
    protected override string DefaultBaseUrl => "https://localhost:5005";
    protected override string LoginPath => "/login";

    protected override async Task NavigateToLoginPageAsync(IPage page)
    {
        await NavigateToHomePageAsync(page);
        await OnPageNavigatedAsync(page);

        if (!page.Url.EndsWith(LoginPath))
        {
            try
            {
                var loginLink = page.GetByRole(AriaRole.Link, new() { Name = "Login" });
                await loginLink.ClickAsync(new() { Timeout = 10000 });
                await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(LoginPath), new() { Timeout = 30000 });
            }
            catch (System.Exception)
            {
                // Fallback to direct navigation if click fails
                await NavigateToAsync(page, LoginPath);
            }
        }
    }

    protected override Task OnPageNavigatedAsync(IPage page) => WaitForSpaProxyAsync(page);

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
    public async Task ClickLogin_NavigatesToLoginPage()
    {
        var page = await CreatePageAsync();

        await NavigateToHomePageAsync(page);
        await OnPageNavigatedAsync(page);

        // The app might automatically redirect to /login due to 401 from profile API
        if (!page.Url.Contains("/login"))
        {
            try
            {
                var loginLink = page.GetByRole(AriaRole.Link, new() { Name = "Login" });
                await loginLink.ClickAsync(new() { Timeout = 5000 });
            }
            catch (System.Exception)
            {
                // Ignore click failure if we already navigated or are navigating
            }
        }

        await Assertions.Expect(page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex("/login"), new() { Timeout = 30000 });
    }

    [Fact]
    public async Task SelectCas_RedirectsToKeycloak()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var casButton = page.GetByRole(AriaRole.Button, new() { Name = "CAS" });
        await Assertions.Expect(casButton).ToBeVisibleAsync(new() { Timeout = 30000 });

        await casButton.ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex("auth\\.dev\\.local"), new() { Timeout = 30000 });
    }

    [Fact]
    public async Task FullLoginFlow_WithCas_ShowsUserInfo()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var loggedIn = await LoginWithCasReactAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Hello,", new() { Timeout = 15000 });
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .Not.ToContainTextAsync("anonymous", new() { Timeout = 15000 });
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Logout" }))
            .ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    [Fact]
    public async Task AuthenticatedUser_CanSeeUserDetails()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var loggedIn = await LoginWithCasReactAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.Locator("dt:text('ID')")).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Assertions.Expect(page.Locator("dt:text('Email')")).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    [Fact]
    public async Task AuthenticatedUser_CanAccessHomePage()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var loggedIn = await LoginWithCasReactAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await NavigateToHomePageAsync(page);
        await OnPageNavigatedAsync(page);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Hello,", new() { Timeout = 15000 });
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Logout" }))
            .ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    [Fact]
    public async Task Logout_ReturnsToAnonymousState()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var loggedIn = await LoginWithCasReactAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await page.WaitForLoadStateAsync(LoadState.Load);

        await page.GetByRole(AriaRole.Button, new() { Name = "Logout" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Hello, anonymous", new() { Timeout = 30000 });
    }

    [Fact]
    public async Task AuthenticatedUser_StaysLoggedIn_AfterNavigation()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);

        var loggedIn = await LoginWithCasReactAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await NavigateToHomePageAsync(page);
        await OnPageNavigatedAsync(page);
        var username = await page.GetByRole(AriaRole.Heading, new() { Level = 1 }).TextContentAsync();

        // Navigate back to home page to verify session persistence
        await NavigateToHomePageAsync(page);
        await OnPageNavigatedAsync(page);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync(username ?? string.Empty, new() { Timeout = 15000 });
    }
    /// <summary>
    /// React sample uses Button instead of Link for CAS login.
    /// </summary>
    private async Task<bool> LoginWithCasReactAsync(IPage page)
    {
        var casButton = page.GetByRole(AriaRole.Button, new() { Name = "CAS" });
        await Assertions.Expect(casButton).ToBeVisibleAsync(new() { Timeout = 30000 });

        await casButton.ClickAsync();

        var keycloakPage = new KeycloakLoginPage(page);
        if (await keycloakPage.IsOnKeycloakLoginPageAsync())
        {
            await keycloakPage.LoginAsync(TestUsername, TestPassword);
            await keycloakPage.WaitForRedirectAsync(BaseUrl);
        }

        return true;
    }

    private async Task WaitForSpaProxyAsync(IPage page)
    {
        // SPA proxy launch page has a specific title and h1
        // We wait until the "Launching the SPA proxy..." heading is gone or the target content is visible
        try
        {
            await page.WaitForFunctionAsync("() => !document.title.includes('SPA proxy launch page') && !document.body.innerText.includes('Launching the SPA proxy')", options: new() { Timeout = 60000 });
            // Wait for any child of #root to exist (indicating SPA has rendered something)
            await page.WaitForSelectorAsync("#root > *", new() { State = WaitForSelectorState.Visible, Timeout = 30000 });
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Warning: WaitForSpaProxyAsync timed out or failed: {ex.Message}");
        }
    }
}

using GSS.Authentication.CAS.E2E.Tests.Fixtures;
using GSS.Authentication.CAS.E2E.Tests.PageObjects;

namespace GSS.Authentication.CAS.E2E.Tests.Tests;

/// <summary>
/// E2E tests for AspNetCoreReactSample.
/// These tests require the sample application to be running with Keycloak.
/// </summary>
public class AspNetCoreReactSampleTests(PlaywrightFixture playwright) : SampleTestsBase(playwright)
{
    protected override string BaseUrl => "https://localhost:5005";
    protected override string LoginPath => "/login";

    [Fact]
    public async Task HomePage_ShowsAnonymousMessage_WhenNotAuthenticated()
    {
        var page = await CreatePageAsync();

        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Hello, anonymous");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Login" }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task LoginPage_ShowsAuthenticationSchemes()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Choose an authentication scheme");

        var casButton = page.GetByRole(AriaRole.Button, new() { Name = "CAS" });
        var oidcButton = page.GetByRole(AriaRole.Button, new() { Name = "OpenIdConnect" });

        var hasCas = await casButton.IsVisibleAsync();
        var hasOidc = await oidcButton.IsVisibleAsync();
        Assert.True(hasCas || hasOidc, "Should have at least one authentication scheme");
    }

    [Fact]
    public async Task ClickLogin_NavigatesToLoginPage()
    {
        var page = await CreatePageAsync();

        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex("/login"));
    }

    [Fact]
    public async Task SelectCas_RedirectsToKeycloak()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var casButton = page.GetByRole(AriaRole.Button, new() { Name = "CAS" });
        var isVisible = await casButton.IsVisibleAsync();
        Assert.SkipWhen(!isVisible, "CAS authentication scheme not available");

        await casButton.ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex("auth\\.dev\\.local"));
    }

    [Fact]
    public async Task FullLoginFlow_WithCas_ShowsUserInfo()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var loggedIn = await LoginWithCasReactAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Hello,");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .Not.ToContainTextAsync("anonymous");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Logout" }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task AuthenticatedUser_CanSeeUserDetails()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var loggedIn = await LoginWithCasReactAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.Locator("dt:text('ID')")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("dt:text('Email')")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Logout_ReturnsToAnonymousState()
    {
        var page = await CreatePageAsync();

        await NavigateToLoginPageAsync(page);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var loggedIn = await LoginWithCasReactAsync(page);
        Assert.SkipWhen(!loggedIn, "CAS authentication scheme not available");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new() { Name = "Logout" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Hello, anonymous");
    }

    /// <summary>
    /// React sample uses Button instead of Link for CAS login.
    /// </summary>
    private async Task<bool> LoginWithCasReactAsync(IPage page)
    {
        var casButton = page.GetByRole(AriaRole.Button, new() { Name = "CAS" });
        if (!await casButton.IsVisibleAsync())
        {
            return false;
        }

        await casButton.ClickAsync();

        var keycloakPage = new KeycloakLoginPage(page);
        if (await keycloakPage.IsOnKeycloakLoginPageAsync())
        {
            await keycloakPage.LoginAsync(TestUsername, TestPassword);
            await keycloakPage.WaitForRedirectAsync(BaseUrl);
        }

        return true;
    }
}

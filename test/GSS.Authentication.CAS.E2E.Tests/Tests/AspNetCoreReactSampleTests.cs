using GSS.Authentication.CAS.E2E.Tests.Fixtures;
using GSS.Authentication.CAS.E2E.Tests.PageObjects;

namespace GSS.Authentication.CAS.E2E.Tests.Tests;

/// <summary>
/// E2E tests for AspNetCoreReactSample.
/// These tests require the sample application to be running with Keycloak.
/// Run manually in DevContainers environment.
/// </summary>
public class AspNetCoreReactSampleTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _playwright;
    private const string BaseUrl = "https://localhost:5005";

    public AspNetCoreReactSampleTests(PlaywrightFixture playwright)
    {
        _playwright = playwright;
    }

    [Fact]
    public async Task HomePage_ShowsAnonymousMessage_WhenNotAuthenticated()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Hello, anonymous");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Login" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task LoginPage_ShowsAuthenticationSchemes()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{BaseUrl}/login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Choose an authentication scheme");

        var casButton = page.GetByRole(AriaRole.Button, new() { Name = "CAS" });
        var oidcButton = page.GetByRole(AriaRole.Button, new() { Name = "OpenIdConnect" });

        var hasCas = await casButton.IsVisibleAsync();
        var hasOidc = await oidcButton.IsVisibleAsync();
        Assert.True(hasCas || hasOidc, "Should have at least one authentication scheme");
    }

    [Fact]
    public async Task ClickLogin_NavigatesToLoginPage()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/login"));
    }

    [Fact]
    public async Task SelectCas_RedirectsToKeycloak()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var casButton = page.GetByRole(AriaRole.Button, new() { Name = "CAS" });
        if (await casButton.IsVisibleAsync())
        {
            await casButton.ClickAsync();

            await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("auth\\.dev\\.local"));
        }
    }

    [Fact]
    public async Task FullLoginFlow_WithCas_ShowsUserInfo()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var casButton = page.GetByRole(AriaRole.Button, new() { Name = "CAS" });
        if (!await casButton.IsVisibleAsync())
        {
            return;
        }

        await casButton.ClickAsync();

        var keycloakPage = new KeycloakLoginPage(page);
        if (await keycloakPage.IsOnKeycloakLoginPageAsync())
        {
            await keycloakPage.LoginAsync("test", "test");
            await keycloakPage.WaitForRedirectAsync(BaseUrl);
        }

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Hello,");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).Not.ToContainTextAsync("anonymous");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Logout" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AuthenticatedUser_CanSeeUserDetails()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var casButton = page.GetByRole(AriaRole.Button, new() { Name = "CAS" });
        if (!await casButton.IsVisibleAsync())
        {
            return;
        }

        await casButton.ClickAsync();

        var keycloakPage = new KeycloakLoginPage(page);
        if (await keycloakPage.IsOnKeycloakLoginPageAsync())
        {
            await keycloakPage.LoginAsync("test", "test");
            await keycloakPage.WaitForRedirectAsync(BaseUrl);
        }

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.Locator("dt:text('ID')")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("dt:text('Email')")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Logout_ReturnsToAnonymousState()
    {
        await using var context = await _playwright.CreateContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var casButton = page.GetByRole(AriaRole.Button, new() { Name = "CAS" });
        if (!await casButton.IsVisibleAsync())
        {
            return;
        }

        await casButton.ClickAsync();

        var keycloakPage = new KeycloakLoginPage(page);
        if (await keycloakPage.IsOnKeycloakLoginPageAsync())
        {
            await keycloakPage.LoginAsync("test", "test");
            await keycloakPage.WaitForRedirectAsync(BaseUrl);
        }

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new() { Name = "Logout" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Hello, anonymous");
    }
}

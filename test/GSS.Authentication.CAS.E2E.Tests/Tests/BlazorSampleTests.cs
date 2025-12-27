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
}

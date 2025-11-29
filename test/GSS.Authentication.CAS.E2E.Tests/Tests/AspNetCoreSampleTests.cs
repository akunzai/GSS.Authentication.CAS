using GSS.Authentication.CAS.E2E.Tests.Fixtures;

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
}

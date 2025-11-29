using GSS.Authentication.CAS.E2E.Tests.Fixtures;
using GSS.Authentication.CAS.E2E.Tests.PageObjects;

namespace GSS.Authentication.CAS.E2E.Tests;

public abstract class SampleTestsBase : IClassFixture<PlaywrightFixture>
{
    protected const string TestUsername = "test";
    protected const string TestPassword = "test";

    protected readonly PlaywrightFixture Playwright;

    protected SampleTestsBase(PlaywrightFixture playwright)
    {
        Playwright = playwright;
    }

    protected abstract string BaseUrl { get; }
    protected virtual string LoginPath => "/Account/Login";

    protected async Task<IPage> CreatePageAsync()
    {
        var context = await Playwright.CreateContextAsync();
        return await context.NewPageAsync();
    }

    protected async Task<bool> LoginWithCasAsync(IPage page)
    {
        var loginPage = new LoginPage(page);
        if (!await loginPage.CasButton.IsVisibleAsync())
        {
            return false;
        }

        await loginPage.SelectCasAsync();

        var keycloakPage = new KeycloakLoginPage(page);
        if (await keycloakPage.IsOnKeycloakLoginPageAsync())
        {
            await keycloakPage.LoginAsync(TestUsername, TestPassword);
            await keycloakPage.WaitForRedirectAsync(BaseUrl);
        }

        return true;
    }

    protected async Task NavigateToLoginPageAsync(IPage page)
    {
        await page.GotoAsync($"{BaseUrl}{LoginPath}");
    }
}

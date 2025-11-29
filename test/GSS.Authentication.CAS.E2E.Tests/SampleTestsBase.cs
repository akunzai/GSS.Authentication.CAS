using GSS.Authentication.CAS.E2E.Tests.Fixtures;
using GSS.Authentication.CAS.E2E.Tests.PageObjects;

namespace GSS.Authentication.CAS.E2E.Tests;

public abstract class SampleTestsBase : IClassFixture<PlaywrightFixture>, IAsyncDisposable
{
    protected const string TestUsername = "test";
    protected const string TestPassword = "test";

    protected readonly PlaywrightFixture Playwright;
    private readonly List<IBrowserContext> _contexts = [];

    protected SampleTestsBase(PlaywrightFixture playwright)
    {
        Playwright = playwright;
    }

    protected virtual string BaseUrl => Environment.GetEnvironmentVariable("SAMPLE_BASE_URL") ?? DefaultBaseUrl;
    protected abstract string DefaultBaseUrl { get; }
    protected virtual string LoginPath => "/Account/Login";

    protected async Task<IPage> CreatePageAsync(string? testName = null)
    {
        var context = await Playwright.CreateContextAsync();
        _contexts.Add(context);
        return await context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            if (Environment.GetEnvironmentVariable("PLAYWRIGHT_TRACE") == "true")
            {
                await context.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = $"recordings/traces/{Guid.NewGuid()}.zip"
                });
            }
            await context.CloseAsync();
        }
        GC.SuppressFinalize(this);
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

    protected async Task NavigateToHomePageAsync(IPage page)
    {
        await NavigateToAsync(page, string.Empty);
    }

    protected virtual async Task NavigateToLoginPageAsync(IPage page)
    {
        await NavigateToAsync(page, LoginPath);
    }

    protected async Task NavigateToAsync(IPage page, string path)
    {
        var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) 
            ? path 
            : $"{BaseUrl}{(path.StartsWith('/') ? path : "/" + path)}";
        try
        {
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load });
        }
        catch (Microsoft.Playwright.PlaywrightException ex) when (ex.Message.Contains("ERR_CONNECTION_REFUSED"))
        {
            Assert.SkipWhen(true, $"Sample application at {BaseUrl} is not running.");
        }
    }

    protected virtual Task OnPageNavigatedAsync(IPage page) => Task.CompletedTask;

    // Common Test Methods

    protected async Task VerifyHomePageAnonymouslyAsync(IPage page)
    {
        await NavigateToHomePageAsync(page);
        await OnPageNavigatedAsync(page);

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 }))
            .ToContainTextAsync("Hello, anonymous", new() { Timeout = 30000 });
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Login" }))
            .ToBeVisibleAsync(new() { Timeout = 30000 });
    }

    protected async Task VerifyLoginPageSchemesAsync(IPage page)
    {
        await NavigateToLoginPageAsync(page);
        await OnPageNavigatedAsync(page);

        var loginPage = new LoginPage(page);
        await Assertions.Expect(loginPage.Heading).ToContainTextAsync("Choose an authentication scheme");
        Assert.True(await loginPage.HasAuthenticationSchemesAsync());
    }
}

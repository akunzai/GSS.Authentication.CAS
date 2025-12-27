namespace GSS.Authentication.CAS.E2E.Tests.Fixtures;

public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async ValueTask DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }

    public async Task<IBrowserContext> CreateContextAsync()
    {
        var options = new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            RecordVideoDir = Environment.GetEnvironmentVariable("PLAYWRIGHT_VIDEO") == "true" ? "recordings/videos" : null
        };
        var context = await Browser.NewContextAsync(options);

        if (Environment.GetEnvironmentVariable("PLAYWRIGHT_TRACE") == "true")
        {
            await context.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });
        }

        return context;
    }
}

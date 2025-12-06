namespace GSS.Authentication.CAS.E2E.Tests.PageObjects;

public class HomePage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public HomePage(IPage page, string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public ILocator Heading => _page.GetByRole(AriaRole.Heading, new() { Level = 1 });
    public ILocator LoginButton => _page.GetByRole(AriaRole.Link, new() { Name = "Login" });
    public ILocator LogoutButton => _page.Locator("text=Logout");
    public ILocator ClaimsDetails => _page.Locator("details:has(summary:text('Claims'))");
    public ILocator ClaimsSummary => _page.Locator("summary:text('Claims')");

    public async Task NavigateAsync()
    {
        await _page.GotoAsync(_baseUrl);
    }

    public async Task<string> GetHeadingTextAsync()
    {
        return await Heading.TextContentAsync() ?? string.Empty;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var headingText = await GetHeadingTextAsync();
        return !headingText.Contains("anonymous", StringComparison.OrdinalIgnoreCase);
    }

    public async Task ClickLoginAsync()
    {
        await LoginButton.ClickAsync();
    }

    public async Task ClickLogoutAsync()
    {
        await LogoutButton.ClickAsync();
    }

    public async Task ExpandClaimsAsync()
    {
        await ClaimsSummary.ClickAsync();
    }
}

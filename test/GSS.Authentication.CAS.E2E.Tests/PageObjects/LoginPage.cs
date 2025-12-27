namespace GSS.Authentication.CAS.E2E.Tests.PageObjects;

public class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page)
    {
        _page = page;
    }

    public ILocator Heading => _page.GetByRole(AriaRole.Heading, new() { Level = 1 });
    public ILocator CasButton => _page.Locator("button, a").GetByText("CAS", new() { Exact = true });
    public ILocator OpenIdConnectButton => _page.Locator("button, a").GetByText("OpenIdConnect", new() { Exact = true });

    public async Task<string> GetHeadingTextAsync()
    {
        return await Heading.TextContentAsync() ?? string.Empty;
    }

    public async Task SelectCasAsync()
    {
        await CasButton.ClickAsync();
    }

    public async Task SelectOpenIdConnectAsync()
    {
        await OpenIdConnectButton.ClickAsync();
    }

    public async Task<bool> HasAuthenticationSchemesAsync(int timeout = 10000)
    {
        try
        {
            await CasButton.Or(OpenIdConnectButton).WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeout });
        }
        catch (System.Exception)
        {
            // Ignore timeout
        }
        var casVisible = await CasButton.IsVisibleAsync();
        var oidcVisible = await OpenIdConnectButton.IsVisibleAsync();
        return casVisible || oidcVisible;
    }
}

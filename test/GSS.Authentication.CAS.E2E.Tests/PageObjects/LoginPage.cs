namespace GSS.Authentication.CAS.E2E.Tests.PageObjects;

public class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page)
    {
        _page = page;
    }

    public ILocator Heading => _page.GetByRole(AriaRole.Heading, new() { Level = 1 });
    public ILocator CasButton => _page.GetByRole(AriaRole.Link, new() { Name = "CAS" })
        .Or(_page.GetByRole(AriaRole.Button, new() { Name = "CAS" }));
    public ILocator OpenIdConnectButton => _page.GetByRole(AriaRole.Link, new() { Name = "OpenIdConnect" })
        .Or(_page.GetByRole(AriaRole.Button, new() { Name = "OpenIdConnect" }));

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

    public async Task<bool> HasAuthenticationSchemesAsync()
    {
        var casVisible = await CasButton.IsVisibleAsync();
        var oidcVisible = await OpenIdConnectButton.IsVisibleAsync();
        return casVisible || oidcVisible;
    }
}

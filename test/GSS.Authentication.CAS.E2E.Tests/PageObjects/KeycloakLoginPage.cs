namespace GSS.Authentication.CAS.E2E.Tests.PageObjects;

public class KeycloakLoginPage
{
    private readonly IPage _page;

    public KeycloakLoginPage(IPage page)
    {
        _page = page;
    }

    public ILocator UsernameInput => _page.Locator("#username");
    public ILocator PasswordInput => _page.Locator("#password");
    public ILocator LoginButton => _page.Locator("#kc-login");

    public async Task<bool> IsOnKeycloakLoginPageAsync()
    {
        try
        {
            await UsernameInput.WaitForAsync(new() { Timeout = 5000 });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task LoginAsync(string username, string password)
    {
        await UsernameInput.FillAsync(username);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();
    }

    public async Task WaitForRedirectAsync(string expectedUrlPrefix)
    {
        await _page.WaitForURLAsync(url => url.StartsWith(expectedUrlPrefix), new() { Timeout = 10000 });
    }
}

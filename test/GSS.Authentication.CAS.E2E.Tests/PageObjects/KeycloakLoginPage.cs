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
        // Increase timeout to 30s and allow any port if the prefix matches the host
        // Note: For React, it might redirect from 5005 to 3000 during the process
        var uri = new System.Uri(expectedUrlPrefix);
        var host = uri.Host;
        await _page.WaitForURLAsync(url => 
            url.Contains(host) && (url.Contains(":3000") || url.Contains(":5005") || url.Contains(":5001") || url.Contains(":5003") || url.Contains(uri.Port.ToString())), 
            new() { Timeout = 30000 });
    }
}

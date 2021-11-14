using Microsoft.Owin.Security.DataProtection;

namespace GSS.Authentication.CAS.Owin.Tests;

internal class FakeDataProtectionProvider : IDataProtectionProvider
{
    private readonly IDataProtector _provider;

    public FakeDataProtectionProvider(IDataProtector provider)
    {
        _provider = provider;
    }

    public IDataProtector Create(params string[] purposes)
    {
        return _provider;
    }
}
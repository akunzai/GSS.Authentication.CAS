using System.Security.Principal;

namespace GSS.Authentication.CAS.Security
{
    public interface ICasPrincipal : IPrincipal
    {
        Assertion Assertion { get; }
    }
}

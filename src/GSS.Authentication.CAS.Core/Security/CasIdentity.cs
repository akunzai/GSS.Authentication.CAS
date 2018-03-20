using System.Security.Claims;

namespace GSS.Authentication.CAS.Security
{
    public class CasIdentity : ClaimsIdentity
    {
        public CasIdentity(Assertion assertion, string authenticationType) : base(authenticationType)
        {
            Assertion = assertion;
        }
        
        public Assertion Assertion { get; protected set; }
    }
}
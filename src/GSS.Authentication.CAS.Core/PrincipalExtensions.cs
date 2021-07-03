using System.Security.Claims;
using System.Security.Principal;
using GSS.Authentication.CAS.Security;

namespace GSS.Authentication.CAS
{
    public static class PrincipalExtensions
    {
        public static string GetPrincipalName(this IIdentity identity)
        {
            return identity switch
            {
                CasIdentity casIdentity when !string.IsNullOrEmpty(casIdentity.Assertion.PrincipalName) => casIdentity
                    .Assertion.PrincipalName,
                ClaimsIdentity claimsIdentity when claimsIdentity.HasClaim(claim =>
                    claim.Type == claimsIdentity.NameClaimType) => claimsIdentity
                    .FindFirst(claimsIdentity.NameClaimType)
                    .Value,
                _ => string.Empty
            };
        }

        public static string GetPrincipalName(this IPrincipal principal)
        {
            switch (principal)
            {
                case ICasPrincipal casPrincipal when !string.IsNullOrEmpty(casPrincipal.Assertion.PrincipalName):
                    return casPrincipal.Assertion.PrincipalName;
                case ClaimsPrincipal claimsPrincipal:
                {
                    foreach (var identity in claimsPrincipal.Identities)
                    {
                        if (identity.HasClaim(x => x.Type == identity.NameClaimType))
                        {
                            return identity.FindFirst(identity.NameClaimType).Value;
                        }
                    }

                    break;
                }
            }

            return principal.Identity?.GetPrincipalName() ?? string.Empty;
        }
    }
}
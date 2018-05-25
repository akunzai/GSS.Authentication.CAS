using System.Security.Claims;
using GSS.Authentication.CAS.Security;

namespace System.Security.Principal
{
    public static class PrincipalExtensions
    {
        public static string GetPrincipalName(this IIdentity identity)
        {
            if (identity is CasIdentity casIdentity
                && !string.IsNullOrEmpty(casIdentity.Assertion.PrincipalName))
            {
                return casIdentity.Assertion.PrincipalName;
            }

            if (identity is ClaimsIdentity claimsIdentity
                && claimsIdentity.HasClaim(claim => claim.Type == claimsIdentity.NameClaimType))
            {
                return claimsIdentity.FindFirst(claimsIdentity.NameClaimType).Value;
            }

            return string.Empty;
        }

        public static string GetPrincipalName(this IPrincipal principal)
        {
            if (principal is ICasPrincipal casPrincipal
                && !string.IsNullOrEmpty(casPrincipal.Assertion.PrincipalName))
            {
                return casPrincipal.Assertion.PrincipalName;
            }

            if (principal is ClaimsPrincipal claimsPrincipal)
            {
                foreach (var identity in claimsPrincipal.Identities)
                {
                    if (identity is ClaimsIdentity claimsIdentity && claimsIdentity.HasClaim(x => x.Type == claimsIdentity.NameClaimType))
                    {
                        return claimsIdentity.FindFirst(claimsIdentity.NameClaimType).Value;
                    }
                }
            }

            return principal.Identity.GetPrincipalName();
        }
    }
}

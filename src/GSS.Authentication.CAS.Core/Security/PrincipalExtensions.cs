using System.Security.Claims;
using GSS.Authentication.CAS.Security;

namespace System.Security.Principal
{
    public static class PrincipalExtensions
    {
        public static string GetPrincipalName(this IIdentity identity)
        {
            var casIdentity = identity as CasIdentity;
            if (casIdentity != null && !string.IsNullOrEmpty(casIdentity.Assertion.PrincipalName))
            {
                return casIdentity.Assertion.PrincipalName;
            }
            var claimsIdentity = identity as ClaimsIdentity;
            if (claimsIdentity != null && claimsIdentity.HasClaim(claim=> claim.Type == ClaimTypes.NameIdentifier))
            {
                return claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            }

            return string.Empty;
        }

        public static string GetPrincipalName(this IPrincipal principal)
        {
            var casPrincipal = principal as ICasPrincipal;
            if (casPrincipal != null && !string.IsNullOrEmpty(casPrincipal.Assertion.PrincipalName))
            {
                return casPrincipal.Assertion.PrincipalName;
            }
            var claimsPrincipal = principal as ClaimsPrincipal;
            if (claimsPrincipal != null && claimsPrincipal.HasClaim(x=> x.Type == ClaimTypes.NameIdentifier))
            {
                return claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
            return principal.Identity.GetPrincipalName();
        }
    }
}

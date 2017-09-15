using System;
using GSS.Authentication.CAS;

namespace Microsoft.AspNetCore.Builder
{
    public static class CasAuthenticationExtensions
    {
        [Obsolete("UseCasAuthentication is obsolete. Configure CAS authentication with AddAuthentication().AddCAS in ConfigureServices.", error: true)]
        public static IApplicationBuilder UseCasAuthentication(this IApplicationBuilder app)
        {
            throw new NotSupportedException("This method is no longer supported");
        }

        [Obsolete("UseCasAuthentication is obsolete. Configure CAS authentication with AddAuthentication().AddCAS in ConfigureServices.", error: true)]
        public static IApplicationBuilder UseCasAuthentication(this IApplicationBuilder app, Action<CasAuthenticationOptions> configureOptions)
        {
            throw new NotSupportedException("This method is no longer supported");
        }

        [Obsolete("UseCasAuthentication is obsolete. Configure CAS authentication with AddAuthentication().AddCAS in ConfigureServices.", error: true)]
        public static IApplicationBuilder UseCasAuthentication(this IApplicationBuilder app, CasAuthenticationOptions options)
        {
            throw new NotSupportedException("This method is no longer supported");
        }
    }
}

using System;
using GSS.Authentication.CAS.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Microsoft.AspNetCore.Builder
{
    public static class CasSingleSignOutExtensions
    {
        public static IApplicationBuilder UseCasSingleSignOut(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            return app.UseMiddleware<CasSingleSignOutMiddleware>();
        }

        public static IApplicationBuilder UseCasSingleSignOut(this IApplicationBuilder app, ITicketStore store)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (store == null) throw new ArgumentNullException(nameof(store));
            return app.UseMiddleware<CasSingleSignOutMiddleware>(store);
        }
    }
}

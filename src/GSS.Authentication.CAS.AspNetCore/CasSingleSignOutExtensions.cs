using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;

namespace GSS.Authentication.CAS.AspNetCore
{
    /// <summary>
    /// Extension methods for using <see cref="CasSingleSignOutMiddleware"/>
    /// </summary>
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

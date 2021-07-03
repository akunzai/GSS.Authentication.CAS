using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;

namespace GSS.Authentication.CAS.AspNetCore
{
    /// <summary>
    /// Extension methods for using <see cref="CasSingleLogoutMiddleware"/>
    /// </summary>
    public static class CasSingleLogoutExtensions
    {
        [Obsolete("Use UseCasSingleLogout instead")]
        public static IApplicationBuilder UseCasSingleSignOut(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            return app.UseMiddleware<CasSingleLogoutMiddleware>();
        }

        [Obsolete("Use UseCasSingleLogout instead")]
        public static IApplicationBuilder UseCasSingleSignOut(this IApplicationBuilder app, ITicketStore store)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (store == null) throw new ArgumentNullException(nameof(store));
            return app.UseMiddleware<CasSingleLogoutMiddleware>(store);
        }

        public static IApplicationBuilder UseCasSingleLogout(this IApplicationBuilder app, ITicketStore? store = null)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            return app.UseMiddleware<CasSingleLogoutMiddleware>(store);
        }
    }
}

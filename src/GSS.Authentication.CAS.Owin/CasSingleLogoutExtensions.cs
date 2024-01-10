using System;
using Microsoft.Owin.Security.Cookies;
using Owin;

namespace GSS.Authentication.CAS.Owin
{
    public static class CasSingleLogoutExtensions
    {
        [Obsolete("Use UseCasSingleLogout instead")]
        public static IAppBuilder UseCasSingleSignOut(this IAppBuilder app, IAuthenticationSessionStore store)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            return app.Use<CasSingleLogoutMiddleware>(app, store);
        }

        public static IAppBuilder UseCasSingleLogout(this IAppBuilder app, IAuthenticationSessionStore store)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            return app.Use<CasSingleLogoutMiddleware>(app, store);
        }
    }
}

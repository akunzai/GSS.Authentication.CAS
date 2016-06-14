using System;
using GSS.Authentication.CAS.Owin;
using Microsoft.Owin.Security.Cookies;

namespace Owin
{
    public static class CasSingleSignOutExtensions
    {
        public static IAppBuilder UseCasSingleSignOut(this IAppBuilder app, IAuthenticationSessionStore store)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (store == null) throw new ArgumentNullException(nameof(store));
            return app.Use<CasSingleSignOutMiddleware>(app, store);
        }
    }
}

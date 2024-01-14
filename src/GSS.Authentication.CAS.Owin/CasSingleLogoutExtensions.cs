using System;
using Microsoft.Owin.Security.Cookies;
using Owin;

namespace GSS.Authentication.CAS.Owin
{
    public static class CasSingleLogoutExtensions
    {
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

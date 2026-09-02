using System;
using Microsoft.Owin.Security.Cookies;
using Owin;

namespace GSS.Authentication.CAS.Owin
{
    /// <summary>
    /// Extension methods for using <see cref="CasSingleLogoutMiddleware"/>
    /// </summary>
    public static class CasSingleLogoutExtensions
    {
        /// <summary>
        /// Adds <see cref="CasSingleLogoutMiddleware"/> to the pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="store">The session store to remove entries from on logout.</param>
        /// <param name="options">Configuration for the middleware. Defaults apply when omitted.</param>
        public static IAppBuilder UseCasSingleLogout(this IAppBuilder app, IAuthenticationSessionStore store,
            CasSingleLogoutOptions? options = null)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            // Always pass a concrete instance (never a literal null) since IAppBuilder.Use<T> resolves the
            // middleware constructor by matching argument count/type, not via a DI container.
            return app.Use<CasSingleLogoutMiddleware>(app, store, options ?? new CasSingleLogoutOptions());
        }
    }
}

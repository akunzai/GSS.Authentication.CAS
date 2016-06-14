using System;
using GSS.Authentication.CAS.Owin;

namespace Owin
{
    public static class CasAuthenticationExtensions
    {
        public static IAppBuilder UseCasAuthentication(this IAppBuilder app, Action<CasAuthenticationOptions> configureOptions = null)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            var options = new CasAuthenticationOptions();
            configureOptions?.Invoke(options);
            return app.UseCasAuthentication(options);
        }

        public static IAppBuilder UseCasAuthentication(this IAppBuilder app, CasAuthenticationOptions options)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (options == null) throw new ArgumentNullException(nameof(options));
            return app.Use<CasAuthenticationMiddleware>(app, options);
        }
    }
}

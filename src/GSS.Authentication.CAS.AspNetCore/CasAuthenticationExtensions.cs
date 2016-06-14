using System;
using GSS.Authentication.CAS.AspNetCore;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    public static class CasAuthenticationExtensions
    {
        public static IApplicationBuilder UseCasAuthentication(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            return app.UseMiddleware<CasAuthenticationMiddleware>();
        }

        public static IApplicationBuilder UseCasAuthentication(this IApplicationBuilder app, Action<CasAuthenticationOptions> configureOptions)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            var options = new CasAuthenticationOptions();
            configureOptions.Invoke(options);
            return app.UseCasAuthentication(options);
        }

        public static IApplicationBuilder UseCasAuthentication(this IApplicationBuilder app, CasAuthenticationOptions options)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (options == null) throw new ArgumentNullException(nameof(options));
            return app.UseMiddleware<CasAuthenticationMiddleware>(Options.Create(options));
        }
    }
}

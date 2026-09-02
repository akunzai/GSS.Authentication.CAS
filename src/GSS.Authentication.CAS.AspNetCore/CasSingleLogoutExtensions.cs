using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS.AspNetCore;

/// <summary>
/// Extension methods for using <see cref="CasSingleLogoutMiddleware"/>
/// </summary>
public static class CasSingleLogoutExtensions
{
    /// <summary>
    /// Adds <see cref="CasSingleLogoutMiddleware"/> to the pipeline.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="store">
    /// The ticket store to remove entries from on logout. Resolved from DI when omitted.
    /// </param>
    /// <param name="options">Configuration for the middleware. Defaults apply when omitted.</param>
    public static IApplicationBuilder UseCasSingleLogout(this IApplicationBuilder app, ITicketStore? store = null,
        CasSingleLogoutOptions? options = null)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        // Omit rather than pass null for unset arguments, so ActivatorUtilities can still resolve the
        // corresponding constructor parameter from DI (a literal null can't be type-matched to a parameter).
        var args = new List<object>();
        if (store != null)
        {
            args.Add(store);
        }

        if (options != null)
        {
            args.Add(Options.Create(options));
        }

        return app.UseMiddleware<CasSingleLogoutMiddleware>(args.ToArray());
    }
}

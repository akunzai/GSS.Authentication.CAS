using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;

namespace GSS.Authentication.CAS.AspNetCore;

/// <summary>
/// Extension methods for using <see cref="CasSingleLogoutMiddleware"/>
/// </summary>
public static class CasSingleLogoutExtensions
{
    public static IApplicationBuilder UseCasSingleLogout(this IApplicationBuilder app, ITicketStore? store = null)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));
        return store == null ? app.UseMiddleware<CasSingleLogoutMiddleware>() : app.UseMiddleware<CasSingleLogoutMiddleware>(store);
    }
}
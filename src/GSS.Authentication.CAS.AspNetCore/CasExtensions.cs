using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS.AspNetCore;

/// <summary>
/// Extension methods for using <see cref="CasAuthenticationHandler"/>
/// </summary>
public static class CasExtensions
{
    public static AuthenticationBuilder AddCAS(this AuthenticationBuilder builder)
        => builder.AddCAS(CasDefaults.AuthenticationType, configureOptions: null);

    public static AuthenticationBuilder AddCAS(this AuthenticationBuilder builder, Action<CasAuthenticationOptions>? configureOptions)
        => builder.AddCAS(CasDefaults.AuthenticationType, configureOptions);

    public static AuthenticationBuilder AddCAS(this AuthenticationBuilder builder, string authenticationScheme, Action<CasAuthenticationOptions>? configureOptions)
        => builder.AddCAS(authenticationScheme, CasDefaults.AuthenticationType, configureOptions);

    public static AuthenticationBuilder AddCAS(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<CasAuthenticationOptions>? configureOptions)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CasAuthenticationOptions>, CasPostConfigureOptions>());
        return builder.AddRemoteScheme<CasAuthenticationOptions, CasAuthenticationHandler>(authenticationScheme, displayName, configureOptions);
    }
}
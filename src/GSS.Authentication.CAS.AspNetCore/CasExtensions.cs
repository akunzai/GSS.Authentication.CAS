using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS.AspNetCore;

/// <summary>
/// Extension methods for using <see cref="CasAuthenticationHandler{TOptions}"/>
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
        => builder.AddCAS<CasAuthenticationOptions, CasAuthenticationHandler<CasAuthenticationOptions>>(authenticationScheme, displayName, configureOptions);

    public static AuthenticationBuilder AddCAS<TOptions, THandler>(this AuthenticationBuilder builder, string authenticationScheme, Action<TOptions>? configureOptions)
        where TOptions : CasAuthenticationOptions, new()
        where THandler : CasAuthenticationHandler<TOptions>
        => builder.AddCAS<TOptions, THandler>(authenticationScheme, CasDefaults.AuthenticationType, configureOptions);

    public static AuthenticationBuilder AddCAS<TOptions, THandler>(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<TOptions>? configureOptions)
        where TOptions : CasAuthenticationOptions, new()
        where THandler : CasAuthenticationHandler<TOptions>
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, CasPostConfigureOptions<TOptions, THandler>>());
        return builder.AddRemoteScheme<TOptions, THandler>(authenticationScheme, displayName, configureOptions);
    }
}
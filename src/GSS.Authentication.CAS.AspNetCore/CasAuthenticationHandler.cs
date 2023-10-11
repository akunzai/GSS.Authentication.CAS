using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS.AspNetCore;

public class CasAuthenticationHandler<TOptions> : RemoteAuthenticationHandler<TOptions>
    where TOptions : CasAuthenticationOptions, new()
{
#if NET8_0_OR_GREATER
    public CasAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }
#else
    public CasAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }
#endif

    protected new CasEvents Events
    {
        get => (CasEvents)base.Events;
        set => base.Events = value;
    }

    /// <summary>
    /// Creates a new instance of the events instance.
    /// </summary>
    /// <returns>A new instance of the events instance.</returns>
    protected override Task<object> CreateEventsAsync()
    {
        return Task.FromResult((object)new CasEvents());
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (string.IsNullOrEmpty(properties.RedirectUri))
        {
            properties.RedirectUri = CurrentUri;
        }

        // CSRF
        GenerateCorrelationId(properties);

        var state = Options.StateDataFormat.Protect(properties);
        var service = BuildRedirectUri(state == null || string.IsNullOrWhiteSpace(state)
            ? Options.CallbackPath
            : $"{Options.CallbackPath}?state={Uri.EscapeDataString(state)}");
        var authorizationEndpoint = $"{Options.CasServerUrlBase}/login?service={Uri.EscapeDataString(service)}";

        var redirectContext = new RedirectContext<CasAuthenticationOptions>(
            Context, Scheme, Options,
            properties, authorizationEndpoint);

        await Options.Events.RedirectToAuthorizationEndpoint(redirectContext).ConfigureAwait(false);
    }

    protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
    {
        var query = Request.Query;
        var state = query["state"];
        var properties = Options.StateDataFormat.Unprotect(state);

        if (properties == null)
        {
            return HandleRequestResult.Fail("The state was missing or invalid.");
        }

        // CSRF
        if (!ValidateCorrelationId(properties))
        {
            return HandleRequestResult.Fail("Correlation failed.");
        }

        var serviceTicket = query["ticket"];

        if (string.IsNullOrEmpty(serviceTicket))
        {
            return HandleRequestResult.Fail("Missing CAS ticket.");
        }

        var service = BuildRedirectUri($"{Options.CallbackPath}?state={Uri.EscapeDataString(state)}");
        ICasPrincipal? principal = null;
        if (Options.ServiceTicketValidator != null)
        {
            principal = await Options.ServiceTicketValidator
                .ValidateAsync(serviceTicket, service, Context.RequestAborted).ConfigureAwait(false);
        }

        if (principal == null)
        {
            return HandleRequestResult.Fail("Missing Validate Principal.");
        }

        if (Options.SaveTokens)
        {
            properties.SetServiceTicket(serviceTicket!);
        }

        try
        {
        var ticket = await CreateTicketAsync(principal as ClaimsPrincipal ?? new ClaimsPrincipal(principal),
            properties, principal.Assertion).ConfigureAwait(false);
            return HandleRequestResult.Success(ticket);
        }
        catch (Exception exception)
        {
            return HandleRequestResult.Fail(exception, properties);
        }
    }

    protected virtual async Task<AuthenticationTicket> CreateTicketAsync(ClaimsPrincipal principal,
        AuthenticationProperties properties, Assertion assertion)
    {
        var context = new CasCreatingTicketContext(principal, properties, Context, Scheme, Options, Options.Backchannel,
            assertion);

        await Events.CreatingTicket(context).ConfigureAwait(false);
        
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(context.Principal);
#else
        if (context.Principal == null)
        {
            throw new ArgumentNullException(nameof(context.Principal));
        }
#endif

        return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
    }
}
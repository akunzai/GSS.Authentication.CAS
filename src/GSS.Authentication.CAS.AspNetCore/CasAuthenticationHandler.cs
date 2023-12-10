using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS.AspNetCore;

public class CasAuthenticationHandler : RemoteAuthenticationHandler<CasAuthenticationOptions>,
    IAuthenticationSignOutHandler
{
    private const string State = "state";

#if NET8_0_OR_GREATER
    public CasAuthenticationHandler(
        IOptionsMonitor<CasAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }
#else
    public CasAuthenticationHandler(
        IOptionsMonitor<CasAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
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

    public override async Task<bool> HandleRequestAsync()
    {
        if (Options.SignedOutCallbackPath.HasValue && Options.SignedOutCallbackPath == Request.Path)
        {
            return await HandleSignOutCallbackAsync();
        }

        return await base.HandleRequestAsync();
    }

    /// <summary>
    /// Redirect user to the identity provider for sign out
    /// </summary>
    /// <param name="properties"></param>
    public async Task SignOutAsync(AuthenticationProperties? properties)
    {
        var target = ResolveTarget(Options.ForwardSignOut);
        if (target != null)
        {
            await Context.SignOutAsync(target, properties).ConfigureAwait(false);
            return;
        }

        properties ??= new AuthenticationProperties();
        if (string.IsNullOrEmpty(properties.RedirectUri))
        {
            properties.RedirectUri = BuildRedirectUriIfRelative(Options.SignedOutRedirectUri);
            if (string.IsNullOrWhiteSpace(properties.RedirectUri))
            {
                properties.RedirectUri = OriginalPathBase + OriginalPath + Request.QueryString;
            }
        }

        var casUrl = new Uri(Options.CasServerUrlBase);
        var redirectUri = UriHelper.BuildAbsolute(
            casUrl.Scheme,
            new HostString(casUrl.Host, casUrl.Port),
            casUrl.LocalPath, Constants.Paths.Logout,
            QueryString.Create(Constants.Parameters.Service,
                QueryHelpers.AddQueryString(BuildRedirectUri(Options.SignedOutCallbackPath), State,
                    Options.StateDataFormat.Protect(properties))));
        var redirectContext = new CasRedirectContext(Context, Scheme, Options, properties, redirectUri);

        await Events.RedirectToAuthorizationEndpoint(redirectContext).ConfigureAwait(false);
        if (redirectContext.Handled)
        {
            return;
        }

        if (!Uri.IsWellFormedUriString(redirectContext.RedirectUri, UriKind.Absolute))
        {
            Logger.LogWarning("The query string for Logout is not a well-formed URI. Redirect URI: '{RedirectUrl}'",
                redirectContext.RedirectUri);
        }

        Response.Redirect(redirectContext.RedirectUri);
    }

    private Task<bool> HandleSignOutCallbackAsync()
    {
        var query = Request.Query;
        var state = query[State];
        var properties = Options.StateDataFormat.Unprotect(state);
        Response.Redirect(!string.IsNullOrEmpty(properties?.RedirectUri)
            ? properties.RedirectUri
            : Options.SignedOutRedirectUri);
        return Task.FromResult(true);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (string.IsNullOrEmpty(properties.RedirectUri))
        {
            properties.RedirectUri = OriginalPathBase + OriginalPath + Request.QueryString;
        }

        var callbackUri = BuildRedirectUri(Options.CallbackPath);

        // CSRF
        GenerateCorrelationId(properties);

        var state = Options.StateDataFormat.Protect(properties);
        if (!string.IsNullOrWhiteSpace(state))
        {
            callbackUri = QueryHelpers.AddQueryString(callbackUri, State, state);
        }

        var redirectUri = Options.CasServerUrlBase + Constants.Paths.Login +
                          $"?{Constants.Parameters.Service}={Uri.EscapeDataString(callbackUri)}";

        var redirectContext = new RedirectContext<CasAuthenticationOptions>(
            Context, Scheme, Options,
            properties, redirectUri);

        await Options.Events.RedirectToAuthorizationEndpoint(redirectContext).ConfigureAwait(false);
    }

    protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
    {
        var query = Request.Query;
        var state = query[State];
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

        var serviceTicket = query[Constants.Parameters.Ticket];

        if (string.IsNullOrEmpty(serviceTicket))
        {
            return HandleRequestResult.Fail("Missing CAS ticket.");
        }

        var callbackUri = BuildRedirectUri($"{Options.CallbackPath}?{State}={Uri.EscapeDataString(state!)}");
        ICasPrincipal? principal = null;
        if (Options.ServiceTicketValidator != null)
        {
            principal = await Options.ServiceTicketValidator
                .ValidateAsync(serviceTicket!, callbackUri, Context.RequestAborted).ConfigureAwait(false);
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

    private async Task<AuthenticationTicket> CreateTicketAsync(ClaimsPrincipal principal,
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

    /// <summary>
    /// Build a redirect path if the given path is a relative path.
    /// </summary>
    private string BuildRedirectUriIfRelative(string uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString))
        {
            return uriString;
        }

        return Uri.TryCreate(uriString, UriKind.Absolute, out _)
            ? uriString
            : BuildRedirectUri(uriString);
    }
}
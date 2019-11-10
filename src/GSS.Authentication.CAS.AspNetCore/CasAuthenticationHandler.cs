using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSS.Authentication.CAS.AspNetCore
{
    public class CasAuthenticationHandler<TOptions> : RemoteAuthenticationHandler<TOptions>
        where TOptions : CasAuthenticationOptions, new()
    {
        public CasAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected HttpClient Backchannel => Options.Backchannel;

        protected new CasEvents Events
        {
            get => (CasEvents)base.Events;
            set => base.Events = value;
        }

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
            var service = BuildRedirectUri($"{Options.CallbackPath}?state={Uri.EscapeDataString(state)}");
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
            var principal = await Options.ServiceTicketValidator.ValidateAsync(serviceTicket, service, Context.RequestAborted).ConfigureAwait(false);

            if (principal == null)
            {
                return HandleRequestResult.Fail("Missing Validate Principal.");
            }

            if (Options.SaveTokens)
            {
                properties.StoreTokens(new List<AuthenticationToken>
                {
                    new AuthenticationToken
                    {
                        Name = "access_token",
                        Value = serviceTicket
                    }
                });
            }

            var ticket = await CreateTicketAsync(principal as ClaimsPrincipal ?? new ClaimsPrincipal(principal), properties, principal.Assertion).ConfigureAwait(false);

            return ticket != null ? HandleRequestResult.Success(ticket) : HandleRequestResult.Fail("Failed to retrieve user information from remote server.");
        }

        protected virtual async Task<AuthenticationTicket> CreateTicketAsync(ClaimsPrincipal principal, AuthenticationProperties properties, Assertion assertion)
        {
            var context = new CasCreatingTicketContext(principal, properties, Context, Scheme, Options, Backchannel, assertion);

            await Events.CreatingTicket(context).ConfigureAwait(false);

            return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
        }
    }
}

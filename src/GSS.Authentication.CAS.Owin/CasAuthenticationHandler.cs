using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Security;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;

namespace GSS.Authentication.CAS.Owin
{
    public class CasAuthenticationHandler : AuthenticationHandler<CasAuthenticationOptions>
    {
        private readonly ILogger _logger;

        public CasAuthenticationHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override async Task<bool> InvokeAsync()
        {
            if (Options.CallbackPath.HasValue && Options.CallbackPath == Request.Path)
            {
                return await InvokeReturnPathAsync().ConfigureAwait(false);
            }
            return false;
        }

        private async Task<bool> InvokeReturnPathAsync()
        {
            AuthenticationTicket? ticket = null;
            Exception? exception = null;
            AuthenticationProperties? properties = null;
            try
            {
                ticket = await AuthenticateAsync().ConfigureAwait(false);
                if (ticket?.Identity == null || !ticket.Identity.IsAuthenticated)
                {
                    exception = new InvalidOperationException("Invalid return state, unable to redirect.");
                    properties = ticket?.Properties;
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                _logger.WriteWarning(exception.Message);
                var errorContext = new CasRemoteFailureContext(Context, exception) { Properties = properties };

                await Options.Provider.RemoteFailure(errorContext).ConfigureAwait(false);

                if (errorContext.Handled)
                {
                    return true;
                }

                if (errorContext.Skipped)
                {
                    return false;
                }

                Response.StatusCode = 500;

                if (errorContext.Failure != null)
                {
                    throw new Exception("An error was encountered while handling the remote login.", errorContext.Failure);
                }
            }

            var context = new CasRedirectToAuthorizationEndpointContext(Context, ticket)
            {
                SignInAsAuthenticationType = Options.SignInAsAuthenticationType,
                RedirectUri = ticket?.Properties.RedirectUri
            };

            if (ticket != null)
            {
                ticket.Properties.RedirectUri = null;
            }

            await Options.Provider.RedirectToAuthorizationEndpoint(context).ConfigureAwait(false);

            if (context.SignInAsAuthenticationType != null && context.Identity != null)
            {
                var signInIdentity = context.Identity;
                if (!string.Equals(signInIdentity.AuthenticationType, context.SignInAsAuthenticationType, StringComparison.Ordinal))
                {
                    signInIdentity = new ClaimsIdentity(signInIdentity.Claims, context.SignInAsAuthenticationType, signInIdentity.NameClaimType, signInIdentity.RoleClaimType);
                }
                Context.Authentication.SignIn(context.Properties, signInIdentity);
            }

            if (!context.IsRequestCompleted && context.RedirectUri != null)
            {
                if (context.Identity == null)
                {
                    // add a redirect hint that sign-in failed in some way
                    context.RedirectUri = WebUtilities.AddQueryString(context.RedirectUri, "error", "access_denied");
                }
                Response.Redirect(context.RedirectUri);
                context.RequestCompleted();
            }

            return context.IsRequestCompleted;
        }

        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            AuthenticationProperties? properties = null;
            var query = Request.Query;
            var state = query.GetValues("state")?.FirstOrDefault() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(state))
            {
                properties = Options.StateDataFormat?.Unprotect(state);
            }

            if (properties == null)
            {
                return new AuthenticationTicket(null, null);
            }

            // Anti-CSRF
            if (!ValidateCorrelationId(Options.CookieManager, properties, _logger))
            {
                return new AuthenticationTicket(null, properties);
            }

            var ticket = query.GetValues("ticket")?.FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrEmpty(ticket))
            {
                _logger.WriteWarning("Missing ticket parameter");
                return new AuthenticationTicket(null, properties);
            }

            var service = BuildReturnTo(state);
            ICasPrincipal? principal = null;

            if (Options.ServiceTicketValidator != null)
            {
                principal = await Options.ServiceTicketValidator.ValidateAsync(ticket, service, Request.CallCancelled).ConfigureAwait(false);
            }

            if (principal == null)
            {
                _logger.WriteError($"Principal missing in [{Options.ServiceTicketValidator?.GetType().FullName}]");
                return new AuthenticationTicket(null, properties);
            }

            if (Options.SaveTokens)
            {
                // store the service_ticket for single logout
                properties.SetServiceTicket(ticket);
            }

            var context = new CasCreatingTicketContext(
                Context,
                principal.Identity as ClaimsIdentity ?? new ClaimsIdentity(principal.Identity),
                properties);

            await Options.Provider.CreatingTicket(context).ConfigureAwait(false);

            return new AuthenticationTicket(context.Identity, context.Properties);
        }

        protected override Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode != 401)
            {
                return Task.CompletedTask;
            }

            var challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);

            if (challenge != null)
            {
                var requestPrefix = Request.Scheme + Uri.SchemeDelimiter + Request.Host;

                var state = challenge.Properties;
                if (string.IsNullOrEmpty(state.RedirectUri))
                {
                    state.RedirectUri = requestPrefix + Request.PathBase + Request.Path + Request.QueryString;
                }

                // Anti-CSRF
                GenerateCorrelationId(Options.CookieManager, state);

                var returnTo = BuildReturnTo(Options.StateDataFormat?.Protect(state));

                var authorizationEndpoint =
                    $"{Options.CasServerUrlBase}/login?service={Uri.EscapeDataString(returnTo)}";

                Response.Redirect(authorizationEndpoint);
            }

            return Task.CompletedTask;
        }

        private string BuildReturnTo(string? state)
        {
            var baseUrl = Options.ServiceUrlBase?.IsAbsoluteUri == true
                ? Options.ServiceUrlBase.AbsoluteUri.TrimEnd('/')
                : $"{Request.Scheme}://{Request.Host}{RequestPathBase}";
            return
                state == null || string.IsNullOrWhiteSpace(state) ? $"{baseUrl}{Options.CallbackPath}" : $"{baseUrl}{Options.CallbackPath}?state={Uri.EscapeDataString(state)}";
        }
    }
}

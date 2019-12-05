using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using Owin;

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

        public async Task<bool> InvokeReturnPathAsync()
        {
            var model = await AuthenticateAsync().ConfigureAwait(false);
            if (model == null)
            {
                _logger.WriteWarning("Invalid return state, unable to redirect.");
                Response.StatusCode = 500;
                return true;
            }

            var context = new CasRedirectToAuthorizationEndpointContext(Context, model)
            {
                SignInAsAuthenticationType = Options.SignInAsAuthenticationType,
                RedirectUri = model.Properties.RedirectUri
            };
            model.Properties.RedirectUri = null;

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
            try
            {
                var query = Request.Query;
                var state = query.GetValues("state")?.FirstOrDefault() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(state))
                {
                    properties = Options.StateDataFormat.Unprotect(state);
                }
                if (properties == null)
                {
                    _logger.WriteWarning("Invalid return state");
                    return new AuthenticationTicket(null, properties);
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
                var principal = await Options.ServiceTicketValidator.ValidateAsync(ticket, service, Request.CallCancelled).ConfigureAwait(false);
                if (principal == null)
                {
                    _logger.WriteError($"Principal missing in [{Options.ServiceTicketValidator.GetType().FullName}]");
                    return new AuthenticationTicket(null, properties);
                }
                if (Options.UseAuthenticationSessionStore)
                {
                    // store serviceTicket for single sign out
                    properties.SetServiceTicket(ticket);
                }
                var context = new CasCreatingTicketContext(
                    Context,
                    principal.Identity as ClaimsIdentity ?? new ClaimsIdentity(principal.Identity),
                    properties);

                await Options.Provider.CreatingTicket(context).ConfigureAwait(false);

                return new AuthenticationTicket(context.Identity, context.Properties);
            }
            catch (Exception ex)
            {
                _logger.WriteError("Authentication failed", ex);
                return new AuthenticationTicket(null, properties);
            }
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

                var returnTo = BuildReturnTo(Options.StateDataFormat.Protect(state));

                var authorizationEndpoint =
                    $"{Options.CasServerUrlBase}/login?service={Uri.EscapeDataString(returnTo)}";

                Response.Redirect(authorizationEndpoint);
            }

            return Task.CompletedTask;
        }

        private string BuildReturnTo(string state)
        {
            var baseUrl = Options.ServiceUrlBase?.IsAbsoluteUri == true
                ? Options.ServiceUrlBase.AbsoluteUri.TrimEnd('/')
                : $"{Request.Scheme}://{Request.Host}{RequestPathBase}";
            return
                $"{baseUrl}{Options.CallbackPath}?state={Uri.EscapeDataString(state)}";
        }
    }
}

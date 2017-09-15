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
        private readonly ILogger logger;

        public CasAuthenticationHandler(ILogger logger)
        {
            this.logger = logger;
        }

        public override async Task<bool> InvokeAsync()
        {
            if (Options.CallbackPath.HasValue && Options.CallbackPath == Request.Path)
            {
                return await InvokeReturnPathAsync();
            }
            return false;
        }

        public async Task<bool> InvokeReturnPathAsync()
        {
            var model = await AuthenticateAsync();
            if (model == null)
            {
                logger.WriteWarning("Invalid return state, unable to redirect.");
                Response.StatusCode = 500;
                return true;
            }

            var context = new CasRedirectToAuthorizationEndpointContext(Context, model)
            {
                SignInAsAuthenticationType = Options.SignInAsAuthenticationType,
                RedirectUri = model.Properties.RedirectUri
            };
            model.Properties.RedirectUri = null;

            await Options.Provider.RedirectToAuthorizationEndpoint(context);

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
            AuthenticationProperties properties = null;
            try
            {
                var query = Request.Query;
                var state = query.GetValues("state")?.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(state))
                {
                    properties = Options.StateDataFormat.Unprotect(state);
                }
                if (properties == null)
                {
                    logger.WriteWarning("Invalid return state");
                    return null;
                }

                // Anti-CSRF
                if (!ValidateCorrelationId(properties, logger))
                {
                    return new AuthenticationTicket(null, properties);
                }

                var ticket = query.GetValues("ticket")?.FirstOrDefault();
                if (string.IsNullOrEmpty(ticket))
                {
                    // No ticket
                    return new AuthenticationTicket(null, properties);
                }
                var service = BuildReturnTo(state);
                var principal = await Options.ServiceTicketValidator.ValidateAsync(ticket, service, Request.CallCancelled);
                
                // No principal
                if (principal == null)
                {
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

                await Options.Provider.CreatingTicket(context);

                return new AuthenticationTicket(context.Identity, context.Properties);
            }
            catch (Exception ex)
            {
                logger.WriteError("Authentication failed", ex);
                return new AuthenticationTicket(null, properties);
            }
        }

        protected override Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode != 401)
            {
                return Task.FromResult(0);
            }

            var challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);

            if (challenge != null)
            {
                string requestPrefix = Request.Scheme + Uri.SchemeDelimiter + Request.Host;

                var state = challenge.Properties;
                if (string.IsNullOrEmpty(state.RedirectUri))
                {
                    state.RedirectUri = requestPrefix + Request.PathBase + Request.Path + Request.QueryString;
                }

                // Anti-CSRF
                GenerateCorrelationId(state);

                var returnTo = BuildReturnTo(Options.StateDataFormat.Protect(state));

                var authorizationEndpoint =
                    $"{Options.CasServerUrlBase}/login?service={Uri.EscapeDataString(returnTo)}";

                Response.Redirect(authorizationEndpoint);
            }

            return Task.FromResult(0);
        }

        private string BuildReturnTo(string state)
        {
            return
                $"{Request.Scheme}://{Request.Host}{RequestPathBase}{Options.CallbackPath}?state={Uri.EscapeDataString(state)}";
        }
    }
}

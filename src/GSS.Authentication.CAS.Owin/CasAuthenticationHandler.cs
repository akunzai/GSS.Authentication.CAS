using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using GSS.Authentication.CAS.Proxy;

namespace GSS.Authentication.CAS.Owin
{
    public class CasAuthenticationHandler : AuthenticationHandler<CasAuthenticationOptions>
    {
        private const string State = "state";
        private readonly ILogger _logger;

        public CasAuthenticationHandler(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Calls InvokeReturnPathAsync
        /// </summary>
        /// <returns>True if the request was handled, false if the next middleware should be invoked.</returns>
        public override async Task<bool> InvokeAsync()
        {
            if (Options.SignedOutCallbackPath.HasValue && Options.SignedOutCallbackPath == Request.Path)
            {
                return await HandleSignOutCallbackAsync();
            }

            if (Options.CallbackPath.HasValue && Options.CallbackPath == Request.Path)
            {
                return await InvokeReturnPathAsync().ConfigureAwait(false);
            }

            if (Options.ProxyCallbackPath.HasValue && Options.ProxyCallbackPath == Request.Path)
            {
                return await HandleProxyCallbackAsync().ConfigureAwait(false);
            }

            return false;
        }

        /// <summary>
        /// Receives the Proxy Granting Ticket (<c>pgtId</c>/<c>pgtIou</c>) CAS delivers to the <c>pgtUrl</c> callback.
        /// See CAS Protocol Specification §2.5.4/§3.3/§3.4.
        /// </summary>
        private async Task<bool> HandleProxyCallbackAsync()
        {
            var query = Request.Query;
            var proxyGrantingTicketId = query[Constants.Parameters.ProxyGrantingTicketId];
            var proxyGrantingTicketIou = query[Constants.Parameters.ProxyGrantingTicketIou];
            if (!string.IsNullOrEmpty(proxyGrantingTicketId) && !string.IsNullOrEmpty(proxyGrantingTicketIou))
            {
                await Options.ProxyGrantingTicketStore
                    .StoreAsync(proxyGrantingTicketIou, proxyGrantingTicketId, Request.CallCancelled)
                    .ConfigureAwait(false);
            }

            Response.StatusCode = 200;
            return true;
        }

        private Task<bool> HandleSignOutCallbackAsync()
        {
            var query = Request.Query;
            var state = query[State];
            var properties = string.IsNullOrEmpty(state) ? null : Options.StateDataFormat.Unprotect(state);
            Response.Redirect(!string.IsNullOrEmpty(properties?.RedirectUri)
                ? properties!.RedirectUri
                : Options.SignedOutRedirectUri);
            return Task.FromResult(true);
        }

        private async Task<bool> InvokeReturnPathAsync()
        {
            AuthenticationTicket? ticket = null;
            Exception? exception = null;
            AuthenticationProperties? properties = null;
            try
            {
                ticket = await AuthenticateAsync().ConfigureAwait(false);
                if (ticket?.Identity is not { IsAuthenticated: true })
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
                    throw new Exception("An error was encountered while handling the remote login.",
                        errorContext.Failure);
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

            if (context is { SignInAsAuthenticationType: not null, Identity: not null })
            {
                var signInIdentity = context.Identity;
                if (!string.Equals(signInIdentity.AuthenticationType, context.SignInAsAuthenticationType,
                        StringComparison.Ordinal))
                {
                    signInIdentity = new ClaimsIdentity(signInIdentity.Claims, context.SignInAsAuthenticationType,
                        signInIdentity.NameClaimType, signInIdentity.RoleClaimType);
                }

                Context.Authentication.SignIn(context.Properties, signInIdentity);
            }

            if (context is { IsRequestCompleted: false, RedirectUri: not null })
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

        /// <summary>
        /// Invoked to process incoming authentication tickets
        /// </summary>
        /// <returns></returns>
        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            AuthenticationProperties? properties = null;
            var query = Request.Query;
            var state = query.GetValues(State)?.FirstOrDefault() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(state))
            {
                properties = Options.StateDataFormat.Unprotect(state);
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

            var ticket = query.GetValues(Constants.Parameters.Ticket)?.FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrEmpty(ticket))
            {
                throw new InvalidOperationException("Missing ticket parameter from query");
            }

            var service = QueryHelpers.AddQueryString(BuildRedirectUri(Options.CallbackPath.Value), State, state);
            var proxyCallbackUrl = Options.ProxyCallbackPath.HasValue
                ? BuildRedirectUri(Options.ProxyCallbackPath.Value)
                : null;
            if (proxyCallbackUrl != null && !proxyCallbackUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _logger.WriteWarning(
                    $"ProxyCallbackPath is configured but the resulting URL '{proxyCallbackUrl}' is not HTTPS; CAS requires pgtUrl to be HTTPS and will not issue a Proxy Granting Ticket to it.");
            }

            var principal = await Options.ServiceTicketValidator
                .ValidateAsync(ticket, service, Request.CallCancelled, proxyCallbackUrl)
                .ConfigureAwait(false);

            if (principal == null)
            {
                throw new InvalidOperationException(
                    $"Missing principal from [{Options.ServiceTicketValidator.GetType().FullName}]");
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

        /// <summary>
        /// Handles SignOut
        /// </summary>
        /// <returns></returns>
        protected override async Task ApplyResponseGrantAsync()
        {
            var signOut = Helper.LookupSignOut(Options.AuthenticationType, Options.AuthenticationMode);
            if (signOut != null)
            {
                AuthenticationTicket? ticket = null;
                var redirectContext = new CasRedirectContext(Context, ticket)
                {
                    RedirectUri = signOut.Properties.RedirectUri
                };
                await Options.Provider.RedirectToIdentityProviderForSignOut(redirectContext).ConfigureAwait(false);
                if (redirectContext.Handled)
                {
                    return;
                }

                var properties = new AuthenticationProperties { RedirectUri = Options.SignedOutRedirectUri };
                if (!string.IsNullOrWhiteSpace(signOut.Properties.RedirectUri))
                {
                    properties.RedirectUri = signOut.Properties.RedirectUri;
                }

                var returnTo = QueryHelpers.AddQueryString(
                    BuildRedirectUriIfRelative(Options.SignedOutCallbackPath.Value), State,
                    Options.StateDataFormat.Protect(properties));
                var logoutUrl = new UriBuilder(Options.CasServerUrlBase);
                logoutUrl.Path += Constants.Paths.Logout;
                var redirectUri =
                    QueryHelpers.AddQueryString(logoutUrl.Uri.AbsoluteUri, Constants.Parameters.Service, returnTo);
                Response.Redirect(redirectUri);
            }
        }

        /// <summary>
        /// Handles SignIn
        /// </summary>
        /// <returns></returns>
        protected override async Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode != 401)
            {
                return;
            }

            var challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);

            if (challenge != null)
            {
                if (Options.Renew && Options.Gateway)
                {
                    throw new InvalidOperationException(
                        $"'{nameof(Options.Renew)}' and '{nameof(Options.Gateway)}' cannot both be set, per the CAS protocol.");
                }

                var requestPrefix = Request.Scheme + Uri.SchemeDelimiter + Request.Host;

                var state = challenge.Properties;
                if (string.IsNullOrEmpty(state.RedirectUri))
                {
                    state.RedirectUri = requestPrefix + Request.PathBase + Request.Path + Request.QueryString;
                }

                // Anti-CSRF
                GenerateCorrelationId(Options.CookieManager, state);

                var returnTo = QueryHelpers.AddQueryString(BuildRedirectUri(Options.CallbackPath.Value), State,
                    Options.StateDataFormat.Protect(state));

                var authorizationEndpoint = QueryHelpers.AddQueryString(
                    $"{Options.CasServerUrlBase.TrimEnd('/')}{Constants.Paths.Login}",
                    Constants.Parameters.Service, returnTo);

                if (Options.Renew)
                {
                    authorizationEndpoint =
                        QueryHelpers.AddQueryString(authorizationEndpoint, Constants.Parameters.Renew, "true");
                }
                else if (Options.Gateway)
                {
                    authorizationEndpoint =
                        QueryHelpers.AddQueryString(authorizationEndpoint, Constants.Parameters.Gateway, "true");
                }

                if (!string.IsNullOrWhiteSpace(Options.Method))
                {
                    authorizationEndpoint = QueryHelpers.AddQueryString(authorizationEndpoint,
                        Constants.Parameters.Method, Options.Method!.Trim());
                }

                if (!string.IsNullOrWhiteSpace(Options.Locale))
                {
                    authorizationEndpoint = QueryHelpers.AddQueryString(authorizationEndpoint,
                        Constants.Parameters.Locale, Options.Locale!.Trim());
                }

                var redirectContext = new CasRedirectContext(Context, null) { RedirectUri = authorizationEndpoint };
                await Options.Provider.RedirectToIdentityProviderForSignIn(redirectContext).ConfigureAwait(false);
                if (redirectContext.Handled)
                {
                    return;
                }

                Response.Redirect(redirectContext.RedirectUri);
            }
        }

        private string BuildRedirectUri(string path)
        {
            var baseUrl = Options.ServiceUrlBase?.IsAbsoluteUri == true
                ? Options.ServiceUrlBase.AbsoluteUri.TrimEnd('/')
                : $"{Request.Scheme}://{Request.Host}{RequestPathBase}";
            return $"{baseUrl}{path}";
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
}

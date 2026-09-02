using System.Net;
using System.Security.Claims;
using GSS.Authentication.CAS.Proxy;
using GSS.Authentication.CAS.Security;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Owin;
using Xunit;

namespace GSS.Authentication.CAS.Owin.Tests
{
    public class CasAuthenticationMiddlewareTests
    {
        private const string CasServerUrlBase = "https://cas.example.org/cas";

        [Fact]
        public async Task AnonymousRequest_WithRootPath_ShouldRedirectToLoginPath()
        {
            // Arrange
            using var server = CreateServer(options => options.CasServerUrlBase = CasServerUrlBase);

            // Act
            using var response = await server.HttpClient.GetAsync("/", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var loginUri = QueryHelpers.AddQueryString(
                new Uri(server.BaseAddress, CookieAuthenticationDefaults.LoginPath.Value).AbsoluteUri,
                CookieAuthenticationDefaults.ReturnUrlParameter, "/");
            Assert.Equal(loginUri, response.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task AnonymousRequest_WithCallbackPath_ShouldThrows()
        {
            // Arrange
            using var server = CreateServer(options => options.CasServerUrlBase = CasServerUrlBase);
            var exception = await Record.ExceptionAsync(async () =>
            {
                // Act
                await server.HttpClient.GetAsync("/signin-cas", TestContext.Current.CancellationToken);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
            Assert.Equal("Invalid return state, unable to redirect.", exception.InnerException!.Message);
        }

        [Fact]
        public async Task SignInChallenge_ShouldRedirectToCasServer()
        {
            // Arrange
            using var server = CreateServer(options => options.CasServerUrlBase = CasServerUrlBase);

            // Act
            using var response = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith(CasServerUrlBase, response.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task SignInChallenge_WithValidTicketAndPrincipal_ShouldResponseWithAuthCookies()
        {
            // Arrange
            var ticketValidator = Substitute.For<IServiceTicketValidator>();
            var ticket = Guid.NewGuid().ToString();
            var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
            ticketValidator
                .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ICasPrincipal?>(principal));
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator;
                options.CasServerUrlBase = CasServerUrlBase;
                options.Provider = new CasAuthenticationProvider
                {
                    OnCreatingTicket = context =>
                    {
                        var assertion = (context.Identity as CasIdentity)?.Assertion;
                        if (assertion == null)
                            return Task.CompletedTask;
                        context.Identity.AddClaim(new Claim(context.Identity.NameClaimType, assertion.PrincipalName));
                        return Task.CompletedTask;
                    }
                };
            });
            using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value, TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl =
                QueryHelpers.AddQueryString(query[Constants.Parameters.Service], Constants.Parameters.Ticket, ticket);

            // Act
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            using var signInResponse = await server.HttpClient.SendAsync(signInRequest, TestContext.Current.CancellationToken);

            // Assert
            var cookies = signInResponse.Headers.GetValues("Set-Cookie").ToList();
            Assert.Contains(cookies,
                x => x.StartsWith(CookieAuthenticationDefaults.CookiePrefix +
                                  CookieAuthenticationDefaults.AuthenticationType));
            Assert.Contains(cookies,
                x => x.StartsWith(
                    $"{CookieAuthenticationDefaults.CookiePrefix}Correlation.{CasDefaults.AuthenticationType}"));
            Assert.Equal("/", signInResponse.Headers.Location.OriginalString);

            using var authorizedRequest = signInResponse.GetRequestWithCookies("/");
            using var authorizedResponse = await server.HttpClient.SendAsync(authorizedRequest, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);
            var bodyText = await authorizedResponse.Content.ReadAsStringAsync();
            Assert.Equal(principal.GetPrincipalName(), bodyText);
            await ticketValidator.Received(1).ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProxyCallback_WithPgtIdAndPgtIou_ShouldStoreInProxyGrantingTicketStore()
        {
            // Arrange
            var store = new InMemoryProxyGrantingTicketStore();
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.ProxyCallbackPath = new Microsoft.Owin.PathString("/proxyCallback");
                options.ProxyGrantingTicketStore = store;
            });

            // Act
            using var response = await server.HttpClient.GetAsync("/proxyCallback?pgtId=PGT-1-abc&pgtIou=PGTIOU-1-abc",
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("PGT-1-abc", await store.GetAsync("PGTIOU-1-abc", TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task SignInChallenge_WithProxyCallbackPathConfigured_ShouldRequestProxyGrantingTicket()
        {
            // Arrange
            var ticketValidator = Substitute.For<IServiceTicketValidator>();
            var ticket = Guid.NewGuid().ToString();
            var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
            ticketValidator
                .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<string>())
                .Returns(Task.FromResult<ICasPrincipal?>(principal));
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator;
                options.CasServerUrlBase = CasServerUrlBase;
                options.ProxyCallbackPath = new Microsoft.Owin.PathString("/proxyCallback");
            });
            using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value, TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl =
                QueryHelpers.AddQueryString(query[Constants.Parameters.Service], Constants.Parameters.Ticket, ticket);

            // Act
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            await server.HttpClient.SendAsync(signInRequest, TestContext.Current.CancellationToken);

            // Assert
            await ticketValidator.Received(1).ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>(),
                Arg.Is<string>(url => url != null && url.EndsWith("/proxyCallback", StringComparison.Ordinal)));
        }

        [Fact]
        public async Task SignInChallenge_WithoutTicketInCallbackQuery_ShouldThrows()
        {
            // Arrange
            var ticketValidator = Substitute.For<IServiceTicketValidator>();
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator;
                options.CasServerUrlBase = CasServerUrlBase;
            });
            using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value, TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl =
                QueryHelpers.AddQueryString(query[Constants.Parameters.Service], Constants.Parameters.Ticket, string.Empty);
            var exception = await Record.ExceptionAsync(async () =>
            {
                // Act
                using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
                await server.HttpClient.SendAsync(signInRequest, TestContext.Current.CancellationToken);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
            Assert.Equal("Missing ticket parameter from query", exception.InnerException!.Message);
        }

        [Fact]
        public async Task SignInChallenge_WithoutValidPrincipal_ShouldThrows()
        {
            // Arrange
            var ticketValidator = Substitute.For<IServiceTicketValidator>();
            var ticket = Guid.NewGuid().ToString();
            ticketValidator
                .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ICasPrincipal?>(null));
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator;
                options.CasServerUrlBase = CasServerUrlBase;
            });
            using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value, TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl =
                QueryHelpers.AddQueryString(query[Constants.Parameters.Service], Constants.Parameters.Ticket, ticket);
            var exception = await Record.ExceptionAsync(async () =>
            {
                // Act
                using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
                await server.HttpClient.SendAsync(signInRequest, TestContext.Current.CancellationToken);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
            Assert.Contains("Missing principal from", exception.InnerException!.Message);
        }

        [Fact]
        public async Task SignInChallenge_WithValidatingException_ShouldThrows()
        {
            // Arrange
            var ticketValidator = Substitute.For<IServiceTicketValidator>();
            var ticket = Guid.NewGuid().ToString();
            ticketValidator
                .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new NotSupportedException("test"));
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator;
                options.CasServerUrlBase = CasServerUrlBase;
            });
            using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value, TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl =
                QueryHelpers.AddQueryString(query[Constants.Parameters.Service], Constants.Parameters.Ticket, ticket);
            var exception = await Record.ExceptionAsync(async () =>
            {
                // Act
                using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
                await server.HttpClient.SendAsync(signInRequest, TestContext.Current.CancellationToken);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
            Assert.IsType<NotSupportedException>(exception.InnerException);
            Assert.Equal("test", exception.InnerException!.Message);
        }

        [Fact]
        public async Task SignInChallenge_WithValidatingExceptionAndHandledResponse_ShouldRedirectToAccessDeniedPath()
        {
            // Arrange
            var ticketValidator = Substitute.For<IServiceTicketValidator>();
            var ticket = Guid.NewGuid().ToString();
            ticketValidator
                .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new NotSupportedException("test"));
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator;
                options.CasServerUrlBase = CasServerUrlBase;
                options.Provider = new CasAuthenticationProvider
                {
                    OnRemoteFailure = context =>
                    {
                        context.Response.Redirect("/Account/ExternalLoginFailure");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });
            using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value, TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl =
                QueryHelpers.AddQueryString(query[Constants.Parameters.Service], Constants.Parameters.Ticket, ticket);

            // Act
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            using var signInResponse = await server.HttpClient.SendAsync(signInRequest, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, signInResponse.StatusCode);
            Assert.Equal("/Account/ExternalLoginFailure", signInResponse.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task SignInChallenge_WithTicketCreatingException_ShouldThrows()
        {
            // Arrange
            var ticketValidator = Substitute.For<IServiceTicketValidator>();
            var ticket = Guid.NewGuid().ToString();
            var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
            ticketValidator
                .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ICasPrincipal?>(principal));
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator;
                options.CasServerUrlBase = CasServerUrlBase;
                options.Provider = new CasAuthenticationProvider
                {
                    OnCreatingTicket = _ => throw new NotSupportedException("test")
                };
            });

            using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value, TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl =
                QueryHelpers.AddQueryString(query[Constants.Parameters.Service], Constants.Parameters.Ticket, ticket);
            var exception = await Record.ExceptionAsync(async () =>
            {
                // Act
                using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
                await server.HttpClient.SendAsync(signInRequest, TestContext.Current.CancellationToken);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
            Assert.IsType<NotSupportedException>(exception.InnerException);
            Assert.Equal("test", exception.InnerException!.Message);
        }

        [Fact]
        public async Task SignInChallenge_WithTicketCreatingExceptionAndHandledResponse_ShouldRedirectToAccessDeniedPath()
        {
            // Arrange
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.Provider = new CasAuthenticationProvider
                {
                    OnCreatingTicket = _ => throw new NotSupportedException("test"),
                    OnRemoteFailure = context =>
                    {
                        context.Response.Redirect("/Account/ExternalLoginFailure");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });
            var ticket = Guid.NewGuid().ToString();
            using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value, TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl =
                QueryHelpers.AddQueryString(query[Constants.Parameters.Service], Constants.Parameters.Ticket, ticket);

            // Act
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            using var signInResponse = await server.HttpClient.SendAsync(signInRequest, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, signInResponse.StatusCode);
            Assert.Equal("/Account/ExternalLoginFailure", signInResponse.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task SignInChallenge_WithoutCorrelationCookie_ShouldThrows()
        {
            // Arrange
            using var server = CreateServer(options => options.CasServerUrlBase = CasServerUrlBase);
            using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value, TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var callbackUrl = query[Constants.Parameters.Service].ToString();
            var exception = await Record.ExceptionAsync(async () =>
            {
                // Act
                await server.HttpClient.GetAsync(callbackUrl, TestContext.Current.CancellationToken);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.Equal("An error was encountered while handling the remote login.", exception.Message);
            Assert.Equal("Invalid return state, unable to redirect.", exception.InnerException!.Message);
        }

        [Fact]
        public async Task SignInChallenge_WithSaveTokens_ShouldStoreServiceTicket()
        {
            // Arrange
            var ticketValidator = Substitute.For<IServiceTicketValidator>();
            var ticket = Guid.NewGuid().ToString();
            var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
            ticketValidator
                .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ICasPrincipal?>(principal));
            string? savedTicket = null;
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator;
                options.CasServerUrlBase = CasServerUrlBase;
                options.SaveTokens = true;
                options.Provider = new CasAuthenticationProvider
                {
                    OnCreatingTicket = context =>
                    {
                        savedTicket = context.Properties.GetServiceTicket();
                        return Task.CompletedTask;
                    }
                };
            });
            using var challengeResponse = await server.HttpClient.GetAsync(CookieAuthenticationDefaults.LoginPath.Value, TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl =
                QueryHelpers.AddQueryString(query[Constants.Parameters.Service], Constants.Parameters.Ticket, ticket);

            // Act
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            using var signInResponse = await server.HttpClient.SendAsync(signInRequest, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, signInResponse.StatusCode);
            Assert.Equal(ticket, savedTicket);
        }

        [Fact]
        public async Task SignInChallenge_WithoutRedirectUri_ShouldReturnToCurrentPath()
        {
            // Arrange
            var ticketValidator = Substitute.For<IServiceTicketValidator>();
            var ticket = Guid.NewGuid().ToString();
            var principal = new CasPrincipal(new Assertion(Guid.NewGuid().ToString()), CasDefaults.AuthenticationType);
            ticketValidator
                .ValidateAsync(ticket, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ICasPrincipal?>(principal));
            using var server = CreateServer(options =>
            {
                options.ServiceTicketValidator = ticketValidator;
                options.CasServerUrlBase = CasServerUrlBase;
            });
            using var challengeResponse = await server.HttpClient.GetAsync("/challenge", TestContext.Current.CancellationToken);
            var query = QueryHelpers.ParseQuery(challengeResponse.Headers.Location.Query);
            var validateUrl =
                QueryHelpers.AddQueryString(query[Constants.Parameters.Service], Constants.Parameters.Ticket, ticket);

            // Act
            using var signInRequest = challengeResponse.GetRequestWithCookies(validateUrl);
            using var signInResponse = await server.HttpClient.SendAsync(signInRequest, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, signInResponse.StatusCode);
            Assert.Equal("http://localhost/challenge", signInResponse.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task SignInChallenge_WithCustomProvider_ShouldAppendCustomQueryParameter()
        {
            // Arrange
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.Provider = new CasAuthenticationProvider
                {
                    OnRedirectToIdentityProviderForSignIn = context =>
                    {
                        context.RedirectUri =
                            QueryHelpers.AddQueryString(context.RedirectUri, "login_hint", "user@example.org");
                        return Task.CompletedTask;
                    }
                };
            });

            // Act
            using var response = await server.HttpClient.GetAsync("/challenge", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var query = QueryHelpers.ParseQuery(response.Headers.Location.Query);
            Assert.Equal("user@example.org", query["login_hint"]);
        }

        [Fact]
        public async Task SignInChallenge_WhenRedirectHandled_ShouldSkipDefaultRedirect()
        {
            // Arrange
            const string customLoginPath = "/custom-login";
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.Provider = new CasAuthenticationProvider
                {
                    OnRedirectToIdentityProviderForSignIn = context =>
                    {
                        context.Response.Redirect(customLoginPath);
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });

            // Act
            using var response = await server.HttpClient.GetAsync("/challenge", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(customLoginPath, response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task SignInChallenge_WithTrailingSlashInCasServerUrlBase_ShouldNotProduceDoubleSlash()
        {
            // Arrange
            using var server = CreateServer(options => options.CasServerUrlBase = CasServerUrlBase + "/");

            // Act
            using var response = await server.HttpClient.GetAsync("/challenge", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.StartsWith(CasServerUrlBase + Constants.Paths.Login + "?", response.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task SignInChallenge_WithRenewMethodLocale_ShouldAppendQueryParametersWithoutGateway()
        {
            // Arrange
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.Renew = true;
                options.Method = "POST";
                options.Locale = "zh_TW";
            });

            // Act
            using var response = await server.HttpClient.GetAsync("/challenge", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var query = QueryHelpers.ParseQuery(response.Headers.Location.Query);
            Assert.Equal("true", query[Constants.Parameters.Renew]);
            Assert.Equal("POST", query[Constants.Parameters.Method]);
            Assert.Equal("zh_TW", query[Constants.Parameters.Locale]);
            Assert.False(query.ContainsKey(Constants.Parameters.Gateway));
        }

        [Fact]
        public async Task SignInChallenge_WithWhitespaceMethodAndLocale_ShouldNotAppendQueryParameters()
        {
            // Arrange
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.Method = " ";
                options.Locale = "\t";
            });

            // Act
            using var response = await server.HttpClient.GetAsync("/challenge", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var query = QueryHelpers.ParseQuery(response.Headers.Location.Query);
            Assert.False(query.ContainsKey(Constants.Parameters.Method));
            Assert.False(query.ContainsKey(Constants.Parameters.Locale));
        }

        [Fact]
        public async Task SignInChallenge_WithPaddedMethodAndLocale_ShouldAppendTrimmedQueryParameters()
        {
            // Arrange
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.Method = " POST ";
                options.Locale = " zh_TW ";
            });

            // Act
            using var response = await server.HttpClient.GetAsync("/challenge", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var query = QueryHelpers.ParseQuery(response.Headers.Location.Query);
            Assert.Equal("POST", query[Constants.Parameters.Method]);
            Assert.Equal("zh_TW", query[Constants.Parameters.Locale]);
        }

        [Fact]
        public async Task SignInChallenge_WithGateway_ShouldAppendGatewayQueryParameter()
        {
            // Arrange
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.Gateway = true;
            });

            // Act
            using var response = await server.HttpClient.GetAsync("/challenge", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var query = QueryHelpers.ParseQuery(response.Headers.Location.Query);
            Assert.Equal("true", query[Constants.Parameters.Gateway]);
            Assert.False(query.ContainsKey(Constants.Parameters.Renew));
        }

        [Fact]
        public async Task SignInChallenge_WithRenewAndGateway_ShouldThrows()
        {
            // Arrange
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.Renew = true;
                options.Gateway = true;
            });

            // Act
            var exception = await Record.ExceptionAsync(() =>
                server.HttpClient.GetAsync("/challenge", TestContext.Current.CancellationToken));

            // Assert
            // Katana's OWIN pipeline may or may not wrap this exception before it reaches HttpClient, so only
            // assert that setting both Renew and Gateway fails the request rather than pinning the exact message.
            Assert.NotNull(exception);
        }

        [Fact]
        public async Task SignInChallenge_WithPlainProvider_ShouldRedirectToCasLoginEndpointUnchanged()
        {
            // Arrange
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.Provider = Substitute.For<ICasAuthenticationProvider>();
            });

            // Act
            using var response = await server.HttpClient.GetAsync("/challenge", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.StartsWith(CasServerUrlBase + Constants.Paths.Login, response.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task SingleSignOut_ShouldRedirectToCasServer()
        {
            // Arrange
            using var server = CreateServer(options => options.CasServerUrlBase = CasServerUrlBase);

            // Act
            using var response = await server.HttpClient.GetAsync("/cas-signout", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var callbackUrl = QueryHelpers.AddQueryString("http://localhost/signout-callback-cas", "state", string.Empty);
            var expectedUrlPrefix =
                QueryHelpers.AddQueryString(CasServerUrlBase + Constants.Paths.Logout, "service", callbackUrl);
            Assert.StartsWith(expectedUrlPrefix, response.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task SingleSignOutCallback_WithState_ShouldRedirectToSignedOutRedirectUri()
        {
            // Arrange
            const string signedOutRedirectUri = "https://app.example.org/logged-out";
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.SignedOutRedirectUri = signedOutRedirectUri;
            });
            using var signOutResponse = await server.HttpClient.GetAsync("/cas-signout", TestContext.Current.CancellationToken);
            var logoutQuery = QueryHelpers.ParseQuery(signOutResponse.Headers.Location.Query);
            var callbackUrl = logoutQuery[Constants.Parameters.Service].ToString();

            // Act
            using var callbackResponse = await server.HttpClient.GetAsync(callbackUrl, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, callbackResponse.StatusCode);
            Assert.Equal(signedOutRedirectUri, callbackResponse.Headers.Location.AbsoluteUri);
        }

        [Fact]
        public async Task SingleSignOutCallback_WithoutState_ShouldRedirectToSignedOutRedirectUri()
        {
            // Arrange
            const string signedOutRedirectUri = "/logged-out";
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.SignedOutRedirectUri = signedOutRedirectUri;
            });

            // Act
            using var response = await server.HttpClient.GetAsync("/signout-callback-cas", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(signedOutRedirectUri, response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task SingleSignOut_WhenRedirectHandled_ShouldSkipCasLogout()
        {
            // Arrange
            const string customLogoutPath = "/custom-signout";
            using var server = CreateServer(options =>
            {
                options.CasServerUrlBase = CasServerUrlBase;
                options.Provider = new CasAuthenticationProvider
                {
                    OnRedirectToIdentityProviderForSignOut = context =>
                    {
                        context.Response.Redirect(customLogoutPath);
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });

            // Act
            using var response = await server.HttpClient.GetAsync("/cas-signout", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(customLogoutPath, response.Headers.Location.OriginalString);
        }

        private static TestServer CreateServer(Action<CasAuthenticationOptions> configureOptions)
        {
            var options = new CasAuthenticationOptions();
            configureOptions.Invoke(options);
            return TestServer.Create(app =>
            {
                app.SetDataProtectionProvider(new FakeDataProtectionProvider(new AesDataProtector("test")));
                app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
                app.UseCookieAuthentication(new CookieAuthenticationOptions
                {
                    LoginPath = CookieAuthenticationDefaults.LoginPath,
                    LogoutPath = CookieAuthenticationDefaults.LogoutPath
                });
                app.UseCasAuthentication(options);
                app.Use(async (context, _) =>
                {
                    var request = context.Request;

                    if (request.Path == CookieAuthenticationDefaults.LoginPath)
                    {
                        context.Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" },
                            CasDefaults.AuthenticationType);
                        return;
                    }

                    if (request.Path.Value == "/challenge")
                    {
                        context.Authentication.Challenge(CasDefaults.AuthenticationType);
                        return;
                    }

                    if (request.Path.Value == "/cas-signout")
                    {
                        context.Authentication.SignOut(CasDefaults.AuthenticationType);
                        return;
                    }

                    if (request.Path == CookieAuthenticationDefaults.LogoutPath)
                    {
                        context.Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
                        return;
                    }

                    var user = context.Authentication.User;

                    // Deny anonymous request beyond this point.
                    if (user?.Identities.Any(identity => identity.IsAuthenticated) != true)
                    {
                        // This is what [Authorize] calls
                        // The cookie middleware will intercept this 401 and redirect to LoginPath
                        context.Authentication.Challenge();
                        return;
                    }

                    // Display authenticated principal name
                    await context.Response.WriteAsync(user.GetPrincipalName());
                });
            });
        }
    }
}

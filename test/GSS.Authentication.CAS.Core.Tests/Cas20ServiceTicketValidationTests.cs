using System.Net;
using System.Security.Authentication;
using System.Text;
using GSS.Authentication.CAS.Validation;
using RichardSzalay.MockHttp;
using Xunit;

namespace GSS.Authentication.CAS.Core.Tests;

public class Cas20ServiceTicketValidationTests
{
    private readonly ICasOptions _options = new CasOptions { CasServerUrlBase = "https://cas.example.org/cas" };

    private const string ServiceUrl = "https://dev.example.test";

    [Fact]
    public async Task ValidateServiceTicketWithSuccessXmlResponse_ShouldReturnPrincipal()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        var proxyGrantingTicketIou = $"PGTIOU-{Guid.NewGuid()}";
        var requestUrl =
            $"{_options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(new StringContent(@$"<cas:serviceResponse xmlns:cas=""http://www.yale.edu/tp/cas"">
    <cas:authenticationSuccess>
        <cas:user>username</cas:user>
        <cas:proxyGrantingTicket>{proxyGrantingTicketIou}</cas:proxyGrantingTicket>
    </cas:authenticationSuccess>
</cas:serviceResponse>", Encoding.UTF8, "application/xml"));
        var validator = new Cas20ServiceTicketValidator(_options, new HttpClient(mockHttp));

        // Act
        var principal = await validator.ValidateAsync(ticket, ServiceUrl, CancellationToken.None);

        //Assert
        Assert.NotNull(principal);
        Assert.NotNull(principal.Assertion);
        Assert.Equal(principal.GetPrincipalName(), principal.Assertion.PrincipalName);
        Assert.Empty(principal.Assertion.Attributes);
        Assert.Equal(proxyGrantingTicketIou, principal.Assertion.ProxyGrantingTicketIou);
        Assert.Empty(principal.Assertion.Proxies);
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ValidateServiceTicketWithProxyCallbackUrl_ShouldAppendPgtUrlParameter()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        const string proxyCallbackUrl = "https://dev.example.test/proxyCallback";
        var requestUrl =
            $"{_options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}&pgtUrl={Uri.EscapeDataString(proxyCallbackUrl)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(new StringContent(@"<cas:serviceResponse xmlns:cas=""http://www.yale.edu/tp/cas"">
    <cas:authenticationSuccess>
        <cas:user>username</cas:user>
    </cas:authenticationSuccess>
</cas:serviceResponse>", Encoding.UTF8, "application/xml"));
        var validator = new Cas20ServiceTicketValidator(_options, new HttpClient(mockHttp));

        // Act
        var principal = await validator.ValidateAsync(ticket, ServiceUrl, CancellationToken.None, proxyCallbackUrl);

        // Assert
        Assert.NotNull(principal);
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ValidateServiceTicketWithSuccessJsonResponse_ShouldReturnPrincipal()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        var requestUrl =
            $"{_options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(new StringContent(
                """
                {"serviceResponse":{"authenticationSuccess":{"user":"username","attributes":{"firstname":["John"],"affiliation":["staff","faculty"]}}}}
                """, Encoding.UTF8, "application/json"));
        var validator = new Cas20ServiceTicketValidator(_options, new HttpClient(mockHttp));

        // Act
        var principal = await validator.ValidateAsync(ticket, ServiceUrl, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(principal);
        Assert.Equal("username", principal.Assertion.PrincipalName);
        Assert.Equal("John", principal.Assertion.Attributes["firstname"]);
        Assert.Equal(["staff", "faculty"], principal.Assertion.Attributes["affiliation"].ToArray());
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ValidateServiceTicketWithSuccessJsonResponse_ShouldReturnProxyGrantingTicketIouAndProxies()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        var requestUrl =
            $"{_options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(new StringContent(
                """
                {"serviceResponse":{"authenticationSuccess":{"user":"username","proxyGrantingTicket":"PGTIOU-1-abc","proxies":["https://proxy1.example.org/pgtUrl"]}}}
                """, Encoding.UTF8, "application/json"));
        var validator = new Cas20ServiceTicketValidator(_options, new HttpClient(mockHttp));

        // Act
        var principal = await validator.ValidateAsync(ticket, ServiceUrl, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(principal);
        Assert.Equal("PGTIOU-1-abc", principal.Assertion.ProxyGrantingTicketIou);
        Assert.Equal(["https://proxy1.example.org/pgtUrl"], principal.Assertion.Proxies);
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ValidateServiceTicketWithFailJsonResponse_ShouldThrowsAuthenticationException()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        var requestUrl =
            $"{_options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(new StringContent(
                """
                {"serviceResponse":{"authenticationFailure":{"code":"INVALID_TICKET","description":"Ticket not recognized"}}}
                """, Encoding.UTF8, "application/json"));
        var validator = new Cas20ServiceTicketValidator(_options, new HttpClient(mockHttp));

        // Act & Assert
        await Assert
            .ThrowsAsync<AuthenticationException>(() =>
                validator.ValidateAsync(ticket, ServiceUrl, TestContext.Current.CancellationToken));
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ValidateServiceTicketWithFailXmlResponse_ShouldThrowsAuthenticationException()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        var requestUrl =
            $"{_options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(new StringContent(@$"<cas:serviceResponse xmlns:cas=""http://www.yale.edu/tp/cas"">
    <cas:authenticationFailure code=""INVALID_TICKET"">
        Ticket {ticket} not recognized
    </cas:authenticationFailure>
</cas:serviceResponse>", Encoding.UTF8, "application/xml"));
        var validator = new Cas20ServiceTicketValidator(_options, new HttpClient(mockHttp));

        // Act & Assert
        await Assert
            .ThrowsAsync<AuthenticationException>(() =>
                validator.ValidateAsync(ticket, ServiceUrl, TestContext.Current.CancellationToken));
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ValidateServiceTicketWithBadResponse_ShouldThrowsHttpRequestException()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        var requestUrl =
            $"{_options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(HttpStatusCode.BadRequest);
        var validator = new Cas20ServiceTicketValidator(_options, new HttpClient(mockHttp));

        // Act & Assert
        await Assert
            .ThrowsAsync<HttpRequestException>(
                () => validator.ValidateAsync(ticket, ServiceUrl, TestContext.Current.CancellationToken));
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }
}
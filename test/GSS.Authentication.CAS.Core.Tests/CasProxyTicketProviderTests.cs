using System.Net;
using System.Security.Authentication;
using System.Text;
using GSS.Authentication.CAS.Proxy;
using RichardSzalay.MockHttp;
using Xunit;

namespace GSS.Authentication.CAS.Core.Tests;

public class CasProxyTicketProviderTests
{
    private readonly ICasOptions _options = new CasOptions { CasServerUrlBase = "https://cas.example.org/cas" };

    private const string ProxyGrantingTicket = "PGT-1-abc123";
    private const string TargetService = "https://backend.example.test";

    [Fact]
    public async Task GetProxyTicketAsync_WithSuccessXmlResponse_ShouldReturnProxyTicket()
    {
        // Arrange
        var requestUrl =
            $"{_options.CasServerUrlBase}/proxy?pgt={ProxyGrantingTicket}&targetService={Uri.EscapeDataString(TargetService)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(new StringContent(@"<cas:serviceResponse xmlns:cas=""http://www.yale.edu/tp/cas"">
    <cas:proxySuccess>
        <cas:proxyTicket>PT-1-abc123</cas:proxyTicket>
    </cas:proxySuccess>
</cas:serviceResponse>", Encoding.UTF8, "application/xml"));
        var provider = new CasProxyTicketProvider(_options, new HttpClient(mockHttp));

        // Act
        var proxyTicket =
            await provider.GetProxyTicketAsync(ProxyGrantingTicket, TargetService,
                TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("PT-1-abc123", proxyTicket);
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetProxyTicketAsync_WithFailXmlResponse_ShouldThrowsAuthenticationException()
    {
        // Arrange
        var requestUrl =
            $"{_options.CasServerUrlBase}/proxy?pgt={ProxyGrantingTicket}&targetService={Uri.EscapeDataString(TargetService)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(new StringContent(@"<cas:serviceResponse xmlns:cas=""http://www.yale.edu/tp/cas"">
    <cas:proxyFailure code=""INVALID_TICKET"">
        Ticket PGT-1-abc123 not recognized
    </cas:proxyFailure>
</cas:serviceResponse>", Encoding.UTF8, "application/xml"));
        var provider = new CasProxyTicketProvider(_options, new HttpClient(mockHttp));

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(() =>
            provider.GetProxyTicketAsync(ProxyGrantingTicket, TargetService,
                TestContext.Current.CancellationToken));
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetProxyTicketAsync_WithBadResponse_ShouldThrowsHttpRequestException()
    {
        // Arrange
        var requestUrl =
            $"{_options.CasServerUrlBase}/proxy?pgt={ProxyGrantingTicket}&targetService={Uri.EscapeDataString(TargetService)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(HttpStatusCode.BadRequest);
        var provider = new CasProxyTicketProvider(_options, new HttpClient(mockHttp));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.GetProxyTicketAsync(ProxyGrantingTicket, TargetService,
                TestContext.Current.CancellationToken));
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }
}

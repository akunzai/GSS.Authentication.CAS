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
        var requestUrl =
            $"{_options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(new StringContent(@$"<cas:serviceResponse xmlns:cas=""http://www.yale.edu/tp/cas"">
    <cas:authenticationSuccess>
        <cas:user>username</cas:user>
        <cas:proxyGrantingTicket>{Guid.NewGuid()}</cas:proxyGrantingTicket>
    </cas:authenticationSuccess>
</cas:serviceResponse>", Encoding.UTF8, "application/xml"));
        var validator = new Cas20ServiceTicketValidator(_options, new HttpClient(mockHttp));

        // Act
        var principal = await validator.ValidateAsync(ticket, ServiceUrl, CancellationToken.None).ConfigureAwait(false);

        //Assert
        Assert.NotNull(principal);
        Assert.NotNull(principal!.Assertion);
        Assert.Equal(principal.GetPrincipalName(), principal.Assertion.PrincipalName);
        Assert.Empty(principal.Assertion.Attributes);
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
                validator.ValidateAsync(ticket, ServiceUrl)).ConfigureAwait(false);
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
                () => validator.ValidateAsync(ticket, ServiceUrl)).ConfigureAwait(false);
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }
}
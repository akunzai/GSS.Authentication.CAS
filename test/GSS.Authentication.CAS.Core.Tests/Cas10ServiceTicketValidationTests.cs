using System.Net;
using GSS.Authentication.CAS.Validation;
using RichardSzalay.MockHttp;
using Xunit;

namespace GSS.Authentication.CAS.Core.Tests;

public class Cas10ServiceTicketValidationTests
{
    private readonly ICasOptions _options = new CasOptions { CasServerUrlBase = "https://cas.example.org/cas" };

    private const string ServiceUrl = "https://dev.example.test";

    [Fact]
    public async Task ValidateServiceTicketWithSuccessPlainResponse_ShouldReturnPrincipal()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        var requestUrl =
            $"{_options.CasServerUrlBase}/validate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond("plain/text", "yes\nusername");
        var validator = new Cas10ServiceTicketValidator(_options, new HttpClient(mockHttp));

        // Act
        var principal = await validator.ValidateAsync(ticket, ServiceUrl, TestContext.Current.CancellationToken);

        //Assert
        Assert.NotNull(principal);
        Assert.NotNull(principal.Assertion);
        Assert.Equal(principal.GetPrincipalName(), principal.Assertion.PrincipalName);
        Assert.Empty(principal.Assertion.Attributes);
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ValidateServiceTicketWithServiceParameter_ShouldSupportEncodedParameters()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        var serviceUrl = $"https://dev.example.test?returnUrl={Uri.EscapeDataString("http://localhost/")}";
        var requestUrl =
            $"{_options.CasServerUrlBase}/validate?ticket={ticket}&service={Uri.EscapeDataString(serviceUrl)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond("plain/text", "yes\nusername");
        var validator = new Cas10ServiceTicketValidator(_options, new HttpClient(mockHttp));

        // Act
        var principal = await validator.ValidateAsync(ticket, serviceUrl, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(principal);
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ValidateServiceTicketWithFailPlainResponse_ShouldReturnNull()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        var requestUrl =
            $"{_options.CasServerUrlBase}/validate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond("plain/text", "no");
        var validator = new Cas10ServiceTicketValidator(_options, new HttpClient(mockHttp));

        // Act
        var principal = await validator.ValidateAsync(ticket, ServiceUrl, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(principal);
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ValidateServiceTicketWithBadResponse_ShouldThrowsHttpRequestException()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        var requestUrl =
            $"{_options.CasServerUrlBase}/validate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(HttpStatusCode.BadRequest);
        var validator = new Cas10ServiceTicketValidator(_options, new HttpClient(mockHttp));

        // Act & Assert
        await Assert
            .ThrowsAsync<HttpRequestException>(
                () => validator.ValidateAsync(ticket, ServiceUrl, TestContext.Current.CancellationToken));
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }
}
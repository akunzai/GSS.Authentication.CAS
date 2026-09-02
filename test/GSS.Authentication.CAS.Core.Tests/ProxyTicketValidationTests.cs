using System.Text;
using GSS.Authentication.CAS.Validation;
using RichardSzalay.MockHttp;
using Xunit;

namespace GSS.Authentication.CAS.Core.Tests;

public class ProxyTicketValidationTests
{
    private readonly ICasOptions _options = new CasOptions { CasServerUrlBase = "https://cas.example.org/cas" };

    private const string ServiceUrl = "https://dev.example.test";

    [Fact]
    public async Task Cas20ProxyTicketValidator_WithProxiesInResponse_ShouldReturnPrincipalWithProxies()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        var requestUrl =
            $"{_options.CasServerUrlBase}/proxyValidate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(new StringContent(@"<cas:serviceResponse xmlns:cas=""http://www.yale.edu/tp/cas"">
    <cas:authenticationSuccess>
        <cas:user>username</cas:user>
        <cas:proxies>
            <cas:proxy>https://proxy2.example.org/pgtUrl</cas:proxy>
            <cas:proxy>https://proxy1.example.org/pgtUrl</cas:proxy>
        </cas:proxies>
    </cas:authenticationSuccess>
</cas:serviceResponse>", Encoding.UTF8, "application/xml"));
        var validator = new Cas20ProxyTicketValidator(_options, new HttpClient(mockHttp));

        // Act
        var principal = await validator.ValidateAsync(ticket, ServiceUrl, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(principal);
        Assert.Equal(["https://proxy2.example.org/pgtUrl", "https://proxy1.example.org/pgtUrl"],
            principal.Assertion.Proxies);
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task Cas30ProxyTicketValidator_WithProxiesInResponse_ShouldReturnPrincipalWithProxies()
    {
        // Arrange
        var ticket = Guid.NewGuid().ToString();
        var requestUrl =
            $"{_options.CasServerUrlBase}/p3/proxyValidate?ticket={ticket}&service={Uri.EscapeDataString(ServiceUrl)}";
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, requestUrl)
            .Respond(new StringContent(@"<cas:serviceResponse xmlns:cas=""http://www.yale.edu/tp/cas"">
    <cas:authenticationSuccess>
        <cas:user>username</cas:user>
        <cas:proxies>
            <cas:proxy>https://proxy1.example.org/pgtUrl</cas:proxy>
        </cas:proxies>
    </cas:authenticationSuccess>
</cas:serviceResponse>", Encoding.UTF8, "application/xml"));
        var validator = new Cas30ProxyTicketValidator(_options, new HttpClient(mockHttp));

        // Act
        var principal = await validator.ValidateAsync(ticket, ServiceUrl, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(principal);
        Assert.Equal(["https://proxy1.example.org/pgtUrl"], principal.Assertion.Proxies);
        mockHttp.VerifyNoOutstandingRequest();
        mockHttp.VerifyNoOutstandingExpectation();
    }
}

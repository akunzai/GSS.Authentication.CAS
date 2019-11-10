using System;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Validation;
using RichardSzalay.MockHttp;
using Xunit;

namespace GSS.Authentication.CAS.Testing.Validation
{
    public class Cas10ServiceTicketValidationTest : IClassFixture<CasFixture>
    {
        private readonly ICasOptions _options;
        private readonly string _service;

        public Cas10ServiceTicketValidationTest(CasFixture fixture)
        {
            _options = fixture.Options;
            _service = fixture.Service;
        }

        [Fact]
        public async Task ValidateServiceTicketWithSuccessPlainResponse_ShouldReturnPrincipal()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_options.CasServerUrlBase}/validate?ticket={ticket}&service={Uri.EscapeDataString(_service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond("plain/text", "yes\nusername");
            var validator = new Cas10ServiceTicketValidator(_options, new HttpClient(mockHttp));

            // Act
            var principal = await validator.ValidateAsync(ticket, _service, CancellationToken.None).ConfigureAwait(false);

            //Assert
            Assert.NotNull(principal);
            Assert.NotNull(principal.Assertion);
            Assert.Equal(principal.GetPrincipalName(), principal.Assertion.PrincipalName);
            Assert.Empty(principal.Assertion.Attributes);
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ValidateServiceTicketWithFailPlainResponse_ShouldReturnNull()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_options.CasServerUrlBase}/validate?ticket={ticket}&service={Uri.EscapeDataString(_service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond("plain/text", "no");
            var validator = new Cas10ServiceTicketValidator(_options, new HttpClient(mockHttp));

            // Act
            var principal = await validator.ValidateAsync(ticket, _service, CancellationToken.None).ConfigureAwait(false);
            Assert.Null(principal);
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ValidateServiceTicketWithBadResponse_ShouldThrowsHttpRequestException()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_options.CasServerUrlBase}/validate?ticket={ticket}&service={Uri.EscapeDataString(_service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond(HttpStatusCode.BadRequest);
            var validator = new Cas10ServiceTicketValidator(_options, new HttpClient(mockHttp));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => validator.ValidateAsync(ticket, _service, CancellationToken.None)).ConfigureAwait(false);
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}

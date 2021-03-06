using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Testing;
using GSS.Authentication.CAS.Validation;
using Microsoft.Extensions.FileProviders;
using RichardSzalay.MockHttp;
using Xunit;

namespace GSS.Authentication.CAS.Core.Tests
{
    public class Cas30ServiceTicketValidationTest : IClassFixture<CasFixture>
    {
        private readonly ICasOptions _options;
        private readonly string _service;
        private readonly IFileProvider _files;

        public Cas30ServiceTicketValidationTest(CasFixture fixture)
        {
            _options = fixture.Options;
            _service = fixture.Service;
            _files = fixture.FileProvider;
        }

        [Fact]
        public async Task ValidateServiceTicketWithSuccessXmlResponse_ShouldReturnPrincipal()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_options.CasServerUrlBase}/p3/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(_service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond(_files.ReadAsHttpContent("Cas30ValidationSuccess.xml", mediaType: "application/xml"));
            var validator = new Cas30ServiceTicketValidator(_options, new HttpClient(mockHttp));

            // Act
            var principal = await validator.ValidateAsync(ticket, _service, CancellationToken.None).ConfigureAwait(false);

            //Assert
            Assert.NotNull(principal);
            Assert.NotNull(principal.Assertion);
            Assert.Equal(principal.GetPrincipalName(), principal.Assertion.PrincipalName);
            Assert.NotEmpty(principal.Assertion.Attributes);
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ValidateServiceTicketWithFailXmlResponse_ShouldThrowsAuthenticationException()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_options.CasServerUrlBase}/p3/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(_service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond(_files.ReadAsHttpContent("Cas20ValidationFail.xml", mediaType: "application/xml"));
            var validator = new Cas30ServiceTicketValidator(_options, new HttpClient(mockHttp));

            // Act & Assert
            await Assert.ThrowsAsync<AuthenticationException>(() => validator.ValidateAsync(ticket, _service, CancellationToken.None)).ConfigureAwait(false);
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ValidateServiceTicketWithBadResponse_ShouldThrowsHttpRequestException()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_options.CasServerUrlBase}/p3/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(_service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond(HttpStatusCode.BadRequest);
            var validator = new Cas30ServiceTicketValidator(_options, new HttpClient(mockHttp));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => validator.ValidateAsync(ticket, _service, CancellationToken.None)).ConfigureAwait(false);
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}

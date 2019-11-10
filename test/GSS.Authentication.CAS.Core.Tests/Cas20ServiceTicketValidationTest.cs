using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Validation;
using Microsoft.Extensions.FileProviders;
using RichardSzalay.MockHttp;
using Xunit;

namespace GSS.Authentication.CAS.Testing.Validation
{
    public class Cas20ServiceTicketValidationTest : IClassFixture<CasFixture>
    {
        private readonly ICasOptions _options;
        private readonly string _service;
        private readonly IFileProvider _files;

        public Cas20ServiceTicketValidationTest(CasFixture fixture)
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
            var requestUrl = $"{_options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(_service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond(_files.ReadAsHttpContent("Cas20ValidationSuccess.xml", mediaType: "application/xml"));
            var validator = new Cas20ServiceTicketValidator(_options, new HttpClient(mockHttp));

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
        public async Task ValidateServiceTicketWithFailXmlResponse_ShouldThrowsAuthenticationException()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(_service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond(_files.ReadAsHttpContent("Cas20ValidationFail.xml", mediaType: "application/xml"));
            var validator = new Cas20ServiceTicketValidator(_options, new HttpClient(mockHttp));

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
            var requestUrl = $"{_options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(_service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond(HttpStatusCode.BadRequest);
            var validator = new Cas20ServiceTicketValidator(_options, new HttpClient(mockHttp));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => validator.ValidateAsync(ticket, _service, CancellationToken.None)).ConfigureAwait(false);
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}

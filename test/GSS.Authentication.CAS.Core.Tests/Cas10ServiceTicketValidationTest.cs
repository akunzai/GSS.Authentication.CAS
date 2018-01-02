using System;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Validation;
using RichardSzalay.MockHttp;
using Xunit;

namespace GSS.Authentication.CAS.Tests.Validation
{
    public class Cas10ServiceTicketValidationTest : IClassFixture<CasFixture>
    {
        private readonly CasFixture _fixture;

        public Cas10ServiceTicketValidationTest(CasFixture fixture)
        {
            _fixture = fixture;
        }
        
        [Fact]
        public async Task ValidateServiceTicket_SuccessAsync()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_fixture.Options.CasServerUrlBase}/validate?ticket={ticket}&service={Uri.EscapeDataString(_fixture.Service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond("plain/text", "yes\nusername");
            var validator = new Cas10ServiceTicketValidator(_fixture.Options, new HttpClient(mockHttp));

            // Act
            var principal = await validator.ValidateAsync(ticket, _fixture.Service, CancellationToken.None);

            //Assert
            Assert.NotNull(principal);
            Assert.NotNull(principal.Assertion);
            Assert.Equal(principal.GetPrincipalName(), principal.Assertion.PrincipalName);
            Assert.Empty(principal.Assertion.Attributes);
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }
        
        [Fact]
        public async Task ValidateServiceTicket_FailAsync()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_fixture.Options.CasServerUrlBase}/validate?ticket={ticket}&service={Uri.EscapeDataString(_fixture.Service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond("plain/text", "no");
            var validator = new Cas10ServiceTicketValidator(_fixture.Options, new HttpClient(mockHttp));

            // Act
            var principal = await validator.ValidateAsync(ticket, _fixture.Service, CancellationToken.None);
            Assert.Null(principal);
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }
        
        [Fact]
        public async Task ValidateServiceTicket_ErrorStatusCodeAsync()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_fixture.Options.CasServerUrlBase}/validate?ticket={ticket}&service={Uri.EscapeDataString(_fixture.Service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond(HttpStatusCode.BadRequest);
            var validator = new Cas10ServiceTicketValidator(_fixture.Options, new HttpClient(mockHttp));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => validator.ValidateAsync(ticket, _fixture.Service, CancellationToken.None));
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}

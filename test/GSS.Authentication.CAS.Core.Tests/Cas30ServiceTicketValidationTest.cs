using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Validation;
using RichardSzalay.MockHttp;
using Xunit;

namespace GSS.Authentication.CAS.Tests.Validation
{
    public class Cas30ServiceTicketValidationTest
    {
        protected string service = "http://localhost";
        protected static ICasOptions options = new CasOptions
        {
            CasServerUrlBase = "http://example.com/cas"
        };

        [Trait("pass", "true")]
        [Fact]
        public async Task ValidateServiceTicket_SuccessAsync()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{options.CasServerUrlBase}/p3/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond("application/xml", ResourceHelper.GetResourceStream("Resources/Cas30ValidationSuccess.xml"));
            var validator = new Cas30ServiceTicketValidator(options, new HttpClient(mockHttp));

            // Act
            var principal = await validator.ValidateAsync(ticket, service, CancellationToken.None);

            //Assert
            Assert.NotNull(principal);
            Assert.NotNull(principal.Assertion);
            Assert.Equal(principal.GetPrincipalName(), principal.Assertion.PrincipalName);
            Assert.NotEmpty(principal.Assertion.Attributes);
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Trait("pass", "true")]
        [Fact]
        public async Task ValidateServiceTicket_FailAsync()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{options.CasServerUrlBase}/p3/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond("application/xml", ResourceHelper.GetResourceStream("Resources/Cas20ValidationFail.xml"));
            var validator = new Cas30ServiceTicketValidator(options, new HttpClient(mockHttp));

            // Act & Assert
            await Assert.ThrowsAsync<AuthenticationException>(() => validator.ValidateAsync(ticket, service, CancellationToken.None));
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Trait("pass", "true")]
        [Fact]
        public async Task ValidateServiceTicket_ErrorStatusCodeAsync()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{options.CasServerUrlBase}/p3/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond(HttpStatusCode.BadRequest);
            var validator = new Cas30ServiceTicketValidator(options, new HttpClient(mockHttp));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => validator.ValidateAsync(ticket, service, CancellationToken.None));
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}

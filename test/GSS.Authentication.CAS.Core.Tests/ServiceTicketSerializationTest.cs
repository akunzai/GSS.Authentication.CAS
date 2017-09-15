using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using Newtonsoft.Json;
using GSS.Authentication.CAS.Security;
using Xunit;

namespace GSS.Authentication.CAS.Core.Tests
{
    public class ServiceTicketSerializationTest
    {
        JsonSerializerSettings SerializerSettings => new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };
        
        [Fact]
        public void SerializeServiceTicket()
        {
            // Arrange
            var assertion = new Assertion("test");
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
            var ticket = new ServiceTicket(Guid.NewGuid().ToString(), assertion, claims.Select(x => new ClaimWrapper(x)), "TEST");

            // Act
            var json = JsonConvert.SerializeObject(ticket, Formatting.Indented, SerializerSettings);

            // Assert
            Assert.NotNull(json);
            Debug.WriteLine(json);
        }
        
        [Fact]
        public void DeserializeServiceTicket()
        {
            // Arrange
            var assertion = new Assertion("test");
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
            var json = JsonConvert.SerializeObject(new ServiceTicket(Guid.NewGuid().ToString(), assertion, claims.Select(x => new ClaimWrapper(x)), "TEST"), SerializerSettings);

            // Act
            var ticket = JsonConvert.DeserializeObject<ServiceTicket>(json, SerializerSettings);

            // Assert
            Assert.NotNull(ticket);
        }
    }
}
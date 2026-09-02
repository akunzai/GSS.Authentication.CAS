using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text.Json;
using System.Xml.Linq;
using GSS.Authentication.CAS.Security;
using Microsoft.Extensions.Primitives;

namespace GSS.Authentication.CAS.Validation
{
    // see https://apereo.github.io/cas/development/protocol/CAS-Protocol-Specification.html#25-servicevalidate-cas-20
    public class Cas20ServiceTicketValidator : CasServiceTicketValidator
    {
        private static readonly XNamespace _namespace = "http://www.yale.edu/tp/cas";
        private static readonly XName _attributes = _namespace + "attributes";
        private static readonly XName _authenticationSuccess = _namespace + "authenticationSuccess";
        private static readonly XName _authenticationFailure = _namespace + "authenticationFailure";
        private static readonly XName _user = _namespace + "user";
        private static readonly XName _proxyGrantingTicket = _namespace + "proxyGrantingTicket";
        private static readonly XName _proxies = _namespace + "proxies";
        private static readonly XName _proxy = _namespace + "proxy";

        private const string JsonMediaType = "application/json";
        private const string JsonServiceResponse = "serviceResponse";
        private const string JsonAuthenticationSuccess = "authenticationSuccess";
        private const string JsonAuthenticationFailure = "authenticationFailure";
        private const string JsonUser = "user";
        private const string JsonAttributes = "attributes";
        private const string JsonProxyGrantingTicket = "proxyGrantingTicket";
        private const string JsonProxies = "proxies";
        private const string JsonFailureCode = "code";
        private const string JsonFailureDescription = "description";

        public Cas20ServiceTicketValidator(
            ICasOptions options,
            HttpClient? httpClient = null)
            : base("serviceValidate", options, httpClient)
        {
        }

        protected Cas20ServiceTicketValidator(
            string suffix,
            ICasOptions options,
            HttpClient? httpClient = null)
            : base(suffix, options, httpClient)
        {
        }

        protected override ICasPrincipal? BuildPrincipal(string responseBody, string? contentType)
        {
            if (string.Equals(contentType, JsonMediaType, StringComparison.OrdinalIgnoreCase))
            {
                return BuildPrincipalFromJson(responseBody);
            }

            var doc = XElement.Parse(responseBody);
            /* On ticket validation failure:
            <cas:serviceResponse xmlns:cas="http://www.yale.edu/tp/cas">
             <cas:authenticationFailure code="INVALID_TICKET">
                Ticket ST-1856339-aA5Yuvrxzpv8Tau1cYQ7 not recognized
              </cas:authenticationFailure>
            </cas:serviceResponse>
            */
            var failureElement = doc.Element(_authenticationFailure);
            if (failureElement != null)
            {
                throw new AuthenticationException(failureElement.Value);
            }

            /* On ticket validation success
            <cas:serviceResponse xmlns:cas="http://www.yale.edu/tp/cas">
                <cas:authenticationSuccess>
                <cas:user>username</cas:user>
                <cas:proxyGrantingTicket>PGTIOU-84678-8a9d...</cas:proxyGrantingTicket>
                </cas:authenticationSuccess>
            </cas:serviceResponse>
            */
            var successElement = doc.Element(_authenticationSuccess);
            if (successElement == null)
                return null;
            var principalName = successElement.Element(_user)?.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(principalName))
                return null;
            var attributes = new Dictionary<string, StringValues>();
            var attributeElements = successElement.Element(_attributes)?.Elements();
            /* User attributes may released in CAS v2 protocol with forward-compatible mode
            <cas:serviceResponse xmlns:cas="http://www.yale.edu/tp/cas">
                <cas:authenticationSuccess>
                  <cas:user>username</cas:user>
                  <cas:attributes>
                    <cas:firstname>John</cas:firstname>
                    <cas:lastname>Doe</cas:lastname>
                    <cas:title>Mr.</cas:title>
                    <cas:email>jdoe @example.org</cas:email>
                    <cas:affiliation>staff</cas:affiliation>
                    <cas:affiliation>faculty</cas:affiliation>
                  </cas:attributes>
                  <cas:proxyGrantingTicket>PGTIOU-84678-8a9d...</cas:proxyGrantingTicket>
                </cas:authenticationSuccess>
            </cas:serviceResponse>
             */
            if (attributeElements != null)
            {
                foreach (var attr in attributeElements)
                {
                    var name = attr.Name.LocalName;
                    attributes[name] = attributes.TryGetValue(name, out var value) ? StringValues.Concat(value, attr.Value)
                        : new StringValues(attr.Value);
                }
            }

            var proxyGrantingTicketIou = successElement.Element(_proxyGrantingTicket)?.Value;
            var proxies = successElement.Element(_proxies)?.Elements(_proxy).Select(e => e.Value).ToList();

            var assertion = new Assertion(principalName, attributes, proxyGrantingTicketIou, proxies);
            return new CasPrincipal(assertion, Options.AuthenticationType);
        }

        /// <summary>
        /// Parses a CAS 3.0 <c>format=JSON</c> response, mirroring the same <c>user</c>/<c>attributes</c>/
        /// failure-code structure as the XML response.
        /// </summary>
        /// <remarks>
        /// On failure:
        /// <code>{"serviceResponse":{"authenticationFailure":{"code":"INVALID_TICKET","description":"..."}}}</code>
        /// On success:
        /// <code>{"serviceResponse":{"authenticationSuccess":{"user":"username","attributes":{"firstname":["John"],"affiliation":["staff","faculty"]}}}}</code>
        /// </remarks>
        private ICasPrincipal? BuildPrincipalFromJson(string responseBody)
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (!doc.RootElement.TryGetProperty(JsonServiceResponse, out var serviceResponse))
                return null;

            if (serviceResponse.TryGetProperty(JsonAuthenticationFailure, out var failureElement))
            {
                var description = failureElement.TryGetProperty(JsonFailureDescription, out var descriptionElement)
                    ? descriptionElement.GetString()
                    : failureElement.TryGetProperty(JsonFailureCode, out var codeElement)
                        ? codeElement.GetString()
                        : null;
                throw new AuthenticationException(description ?? "Ticket validation failed");
            }

            if (!serviceResponse.TryGetProperty(JsonAuthenticationSuccess, out var successElement))
                return null;

            var principalName = successElement.TryGetProperty(JsonUser, out var userElement)
                ? userElement.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(principalName))
                return null;

            var attributes = new Dictionary<string, StringValues>();
            if (successElement.TryGetProperty(JsonAttributes, out var attributesElement))
            {
                foreach (var attribute in attributesElement.EnumerateObject())
                {
                    var values = attribute.Value.ValueKind == JsonValueKind.Array
                        ? attribute.Value.EnumerateArray().Select(v => v.GetString() ?? string.Empty).ToArray()
                        : [attribute.Value.GetString() ?? string.Empty];
                    attributes[attribute.Name] = new StringValues(values);
                }
            }

            var proxyGrantingTicketIou = successElement.TryGetProperty(JsonProxyGrantingTicket, out var pgtElement)
                ? pgtElement.GetString()
                : null;
            List<string>? proxies = null;
            if (successElement.TryGetProperty(JsonProxies, out var proxiesElement) &&
                proxiesElement.ValueKind == JsonValueKind.Array)
            {
                proxies = proxiesElement.EnumerateArray().Select(p => p.GetString() ?? string.Empty).ToList();
            }

            // IsNullOrWhiteSpace isn't annotated [NotNullWhen(false)] on the netstandard2.0 reference assembly.
            var assertion = new Assertion(principalName!, attributes, proxyGrantingTicketIou, proxies);
            return new CasPrincipal(assertion, Options.AuthenticationType);
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace GSS.Authentication.CAS.Security
{
    public class Assertion
    {
        public Assertion(
            string principalName,
            IDictionary<string, StringValues>? attributes = null,
            string? proxyGrantingTicketIou = null,
            IReadOnlyList<string>? proxies = null)
        {
            if (string.IsNullOrWhiteSpace(principalName))
                throw new ArgumentNullException(nameof(principalName));
            PrincipalName = principalName;
            Attributes = attributes ?? new Dictionary<string, StringValues>();
            ProxyGrantingTicketIou = proxyGrantingTicketIou;
            Proxies = proxies ?? Array.Empty<string>();
        }

        public string PrincipalName { get; }

        public IDictionary<string, StringValues> Attributes { get; }

        /// <summary>
        /// The PGTIOU returned by CAS in the <c>cas:proxyGrantingTicket</c> element, correlating to the real
        /// Proxy Granting Ticket delivered separately to the <c>pgtUrl</c> callback.
        /// </summary>
        public string? ProxyGrantingTicketIou { get; }

        /// <summary>
        /// The chain of proxying services, most-recently-visited proxy first, from the <c>cas:proxies</c> element.
        /// Only populated when validating a Proxy Ticket via <c>/proxyValidate</c> or <c>/p3/proxyValidate</c>.
        /// </summary>
        public IReadOnlyList<string> Proxies { get; }
    }
}

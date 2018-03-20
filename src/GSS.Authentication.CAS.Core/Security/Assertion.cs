using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace GSS.Authentication.CAS.Security
{
    public class Assertion
    {
        public Assertion(
            string principalName, 
            IDictionary<string, StringValues> attributes = null,
            DateTimeOffset? validFrom = null,
            DateTimeOffset? validUntil = null)
        {
            if (string.IsNullOrEmpty(principalName)) throw new ArgumentNullException(nameof(principalName));
            PrincipalName = principalName;
            Attributes = attributes ?? new Dictionary<string, StringValues>();
            ValidFrom = validFrom;
            ValidUntil = validUntil;
        }

        public string PrincipalName { get; protected set; }
        
        public IDictionary<string, StringValues> Attributes { get; protected set; }
        
        public DateTimeOffset? ValidFrom { get; protected set; }
        
        public DateTimeOffset? ValidUntil { get; protected set; }
    }
}

using System;
using System.Collections.Generic;

namespace GSS.Authentication.CAS.Security
{
    public class Assertion
    {
        public Assertion(
            string principalName, 
            IDictionary<string, IList<string>> attributes = null,
            DateTimeOffset? validFrom = null,
            DateTimeOffset? validUntil = null)
        {
            if (string.IsNullOrEmpty(principalName)) throw new ArgumentNullException(nameof(principalName));
            PrincipalName = principalName;
            Attributes = attributes ?? new Dictionary<string, IList<string>>();
            ValidFrom = validFrom;
            ValidUntil = validUntil;
        }

        public string PrincipalName { get; protected set; }
        
        public IDictionary<string, IList<string>> Attributes { get; protected set; }
        
        public DateTimeOffset? ValidFrom { get; protected set; }
        
        public DateTimeOffset? ValidUntil { get; protected set; }
    }
}

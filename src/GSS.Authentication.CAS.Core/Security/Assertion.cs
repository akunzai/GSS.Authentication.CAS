using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace GSS.Authentication.CAS.Security
{
    public class Assertion
    {
        public Assertion(
            string principalName,
            IDictionary<string, StringValues>? attributes = null)
        {
            if (string.IsNullOrWhiteSpace(principalName))
                throw new ArgumentNullException(nameof(principalName));
            PrincipalName = principalName;
            Attributes = attributes ?? new Dictionary<string, StringValues>();
        }

        public string PrincipalName { get; }

        public IDictionary<string, StringValues> Attributes { get; }
    }
}

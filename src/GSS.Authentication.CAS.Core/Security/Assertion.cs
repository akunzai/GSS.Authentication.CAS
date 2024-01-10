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
            DateTimeOffset? validFrom = null,
            DateTimeOffset? validUntil = null)
        {
            if (string.IsNullOrWhiteSpace(principalName))
                throw new ArgumentNullException(nameof(principalName));
            PrincipalName = principalName;
            Attributes = attributes ?? new Dictionary<string, StringValues>();
#pragma warning disable CS0618
            ValidFrom = validFrom;
            ValidUntil = validUntil;
#pragma warning restore CS0618
        }

        public string PrincipalName { get; }

        public IDictionary<string, StringValues> Attributes { get; }

        [Obsolete("Will be removed in future release.")]
        public DateTimeOffset? ValidFrom { get; }

        [Obsolete("Will be removed in future release.")]
        public DateTimeOffset? ValidUntil { get; }
    }
}

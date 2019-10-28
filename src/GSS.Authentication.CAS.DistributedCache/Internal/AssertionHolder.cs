using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;
using GSS.Authentication.CAS.Security;

namespace GSS.Authentication.CAS.Internal
{
    /// <summary>
    ///  Assertion Holder for Serialization
    /// </summary>
    internal struct AssertionHolder
    {
        public AssertionHolder(Assertion assertion)
        {
            PrincipalName = assertion.PrincipalName;
            Attributes = assertion.Attributes.ToDictionary(x => x.Key, x => x.Value.ToArray());
            ValidFrom = assertion.ValidFrom;
            ValidUntil = assertion.ValidUntil;
        }

        public string PrincipalName { get; set; }

        public IDictionary<string, string[]> Attributes { get; set; }

        public DateTimeOffset? ValidFrom { get; set; }

        public DateTimeOffset? ValidUntil { get; set; }

        public static explicit operator Assertion(AssertionHolder h) =>
            new Assertion(
                h.PrincipalName,
                h.Attributes.ToDictionary(x => x.Key, x => new StringValues(x.Value)),
                h.ValidFrom,
                h.ValidUntil);
    }
}

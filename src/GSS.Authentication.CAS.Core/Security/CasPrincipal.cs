using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace GSS.Authentication.CAS.Security
{
    public class CasPrincipal : ClaimsPrincipal, ICasPrincipal
    {
        protected IEnumerable<string> roles;

        public CasPrincipal(Assertion assertion, string authenticationType) : this(assertion, authenticationType, null) { }

        public CasPrincipal(Assertion assertion, string authenticationType, IEnumerable<string> roles)
        : base(new CasIdentity(assertion, authenticationType)){
            Assertion = assertion ?? throw new ArgumentNullException(nameof(assertion));
            if (roles != null)
            {
                this.roles = roles;
            }
        }

        #region ICasPrincipal
        public Assertion Assertion { get; protected set; }

        public override bool IsInRole(string role)
        {
            if (roles != null && roles.Contains(role))
            {
                return true;
            }
            return Assertion.Attributes.Keys.Any(attr => Assertion.Attributes[attr].Contains(role));
        }
        #endregion
    }
}

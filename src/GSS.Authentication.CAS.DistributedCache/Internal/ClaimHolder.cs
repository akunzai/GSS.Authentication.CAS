using System.Security.Claims;

namespace GSS.Authentication.CAS.Internal
{
    /// <summary>
    /// Claim Holder for Serialization
    /// </summary>
    internal struct ClaimHolder
    {
        public ClaimHolder(Claim claim)
        {
            Type = claim.Type;
            Value = claim.Value;
            ValueType = claim.ValueType;
            Issuer = claim.Issuer;
            OriginalIssuer = claim.OriginalIssuer;
        }

        public string Type { get; set; }
        public string Value { get; set; }
        public string ValueType { get; set; }
        public string Issuer { get; set; }
        public string OriginalIssuer { get; set; }

        public static explicit operator Claim(ClaimHolder w) => new Claim(w.Type, w.Value, w.ValueType, w.Issuer, w.OriginalIssuer);
    }
}

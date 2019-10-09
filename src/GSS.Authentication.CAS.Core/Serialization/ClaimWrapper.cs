using System.Security.Claims;

namespace GSS.Authentication.CAS
{
    /// <summary>
    /// Claim Wrapper for Serialization
    /// </summary>
    public class ClaimWrapper
    {
        public ClaimWrapper()
        {
        }

        public ClaimWrapper(Claim source)
        {
            Type = source.Type;
            Value = source.Value;
            ValueType = source.ValueType;
            Issuer = source.Issuer;
            OriginalIssuer = source.OriginalIssuer;
        }

        public string? Type { get; set; }
        public string? Value { get; set; }
        public string? ValueType { get; set; }
        public string? Issuer { get; set; }
        public string? OriginalIssuer { get; set; }

        public Claim ToClaim()
        {
            return new Claim(Type, Value, ValueType, Issuer, OriginalIssuer);
        }
    }
}

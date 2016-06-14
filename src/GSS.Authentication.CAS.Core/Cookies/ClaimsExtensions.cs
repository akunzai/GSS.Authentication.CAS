using System.Collections.Generic;

namespace System.Security.Claims
{
    /// <summary>
    /// for Claims Serialization/Deserialization
    /// </summary>
    public static class ClaimsExtensions
    {
        public static IDictionary<string,string> ToDictionary(this IEnumerable<Claim> claims)
        {
            var result = new Dictionary<string, string>();
            foreach(var claim in claims)
            {
                result.Add(claim.Type, claim.Value);
            }
            return result;
        }

        public static void AddClaims(this ClaimsIdentity identity, IDictionary<string, string> map)
        {
            foreach (var pair in map)
            {
                if (identity.HasClaim(pair.Key, pair.Value)) continue;
                identity.AddClaim(new Claim(pair.Key, pair.Value));
            }
        }
    }
}

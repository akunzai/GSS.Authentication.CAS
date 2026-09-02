using System;

namespace GSS.Authentication.CAS
{
    internal static class CasOptionsExtensions
    {
        /// <summary>
        /// The base URI of the CAS server, normalized to always end with a trailing slash so relative endpoint
        /// paths (e.g. <c>serviceValidate</c>, <c>proxy</c>) combine into a single-slash-separated URL.
        /// </summary>
        public static Uri GetBaseUri(this ICasOptions options)
        {
            return new Uri(options.CasServerUrlBase +
#if NETCOREAPP3_1_OR_GREATER
            (options.CasServerUrlBase.EndsWith('/')
#else
            (options.CasServerUrlBase.EndsWith("/")
#endif
                                      ? string.Empty : "/"));
        }
    }
}

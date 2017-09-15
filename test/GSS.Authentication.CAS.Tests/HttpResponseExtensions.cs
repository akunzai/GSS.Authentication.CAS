using System.Collections.Generic;

namespace System.Net.Http
{
    public static class HttpResponseExtensions
    {
        /// <summary>
        /// clone Cookies from Response to Request
        /// </summary>
        /// <param name="response"></param>
        /// <param name="path"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static HttpRequestMessage GetRequest(this HttpResponseMessage response, string path, HttpMethod method = null)
        {
            var request = new HttpRequestMessage(method ?? HttpMethod.Get, path);
            if (!response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> values)) return request;
            var cookies = new List<string>();
            foreach (var value in values)
            {
                var nameValue = value.Split(';')[0];
                var parts = nameValue.Split('=');
                if (string.IsNullOrWhiteSpace(parts[1])) continue;
                cookies.Add(nameValue);
            }
            request.Headers.Add("Cookie", string.Join("; ", cookies.ToArray()));
            return request;
        }
    }
}

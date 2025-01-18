using System.Net.Http;

namespace GSS.Authentication.CAS.Testing;

public static class HttpResponseExtensions
{
    /// <summary>
    /// clone Cookies from Response to Request
    /// </summary>
    /// <param name="response"></param>
    /// <param name="path"></param>
    /// <param name="method"></param>
    /// <returns>HttpRequestMessage with Cookie header</returns>
    public static HttpRequestMessage GetRequestWithCookies(this HttpResponseMessage response,
        string path,
        HttpMethod? method = null)
    {
        var request = new HttpRequestMessage(method ?? HttpMethod.Get, path);
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            return request;
        var cookies = new List<string>();
        foreach (var value in values)
        {
            var nameValue = value.Split(';')[0];
            var parts = nameValue.Split('=');
            if (string.IsNullOrWhiteSpace(parts[1]))
                continue;
            cookies.Add(nameValue);
        }

        request.Headers.Add("Cookie", string.Join("; ", [.. cookies]));
        return request;
    }
}
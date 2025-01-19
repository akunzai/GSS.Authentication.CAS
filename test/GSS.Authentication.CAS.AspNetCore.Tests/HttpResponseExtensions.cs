namespace GSS.Authentication.CAS.AspNetCore.Tests;

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
        var cookies = values.Select(value => value.Split(';')[0])
            .Select(nameValue => new { nameValue, parts = nameValue.Split('=') })
            .Where(t => !string.IsNullOrWhiteSpace(t.parts[1]))
            .Select(t => t.nameValue).ToList();

        request.Headers.Add("Cookie", string.Join("; ", cookies));
        return request;
    }
}
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace GSS.Authentication.CAS.Testing
{
    public static class FileProviderExtensions
    {
        public static JsonSerializerOptions SerializerOptions { get; set; }

        public static Stream ReadAsStream(this IFileProvider fileProvider, string subpath)
        {
            return fileProvider.GetFileInfo(subpath).CreateReadStream();
        }

        public static string ReadAsString(this IFileProvider fileProvider, string subpath)
        {
            using var reader = new StreamReader(fileProvider.ReadAsStream(subpath));
            return reader.ReadToEnd();
        }

        public static async Task<T> ReadAsObjectAsync<T>(this IFileProvider fileProvider, string subpath, Action<T> configurer = null)
        {
            var stream = fileProvider.ReadAsStream(subpath);
            var value = await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions).ConfigureAwait(false);
            configurer?.Invoke(value);
            return value;
        }

        public static HttpContent ReadAsHttpContent(this IFileProvider fileProvider, string subpath, string mediaType, Action<HttpContentHeaders> headersCofigurer = null)
        {
            var stream = fileProvider.ReadAsStream(subpath);
            var httpContent = new StreamContent(stream);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            headersCofigurer?.Invoke(httpContent.Headers);
            return httpContent;
        }

        public static async Task<HttpContent> CreateHttpContentAsync<T>(this T value, string mediaType, Action<HttpContentHeaders> headersCofigurer = null)
        {
            if (value == null)
                return null;
            var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value, SerializerOptions).ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);
            var httpContent = new StreamContent(stream);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            headersCofigurer?.Invoke(httpContent.Headers);
            return httpContent;
        }
    }
}
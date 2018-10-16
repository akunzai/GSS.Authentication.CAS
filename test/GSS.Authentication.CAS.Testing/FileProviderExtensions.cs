using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Extensions.FileProviders
{
    public static class FileProviderExtensions
    {
        public const string JsonMediaType = "application/json";
        public const string StreamMediaType = "application/octet-stream";

        public static JsonSerializerSettings SerializerSettings { get; set; }

        public static Stream ReadAsStream(this IFileProvider fileProvider, string subpath)
        {
            return fileProvider.GetFileInfo(subpath).CreateReadStream();
        }

        public static string ReadAsString(this IFileProvider fileProvider, string subpath)
        {
            using (var reader = new StreamReader(fileProvider.ReadAsStream(subpath)))
            {
                return reader.ReadToEnd();
            }
        }

        public static T ReadAsObject<T>(this IFileProvider fileProvider, string subpath, Action<T> configurer = null)
        {
            var json = fileProvider.ReadAsString(subpath);
            var value = JsonConvert.DeserializeObject<T>(json, SerializerSettings);
            configurer?.Invoke(value);
            return value;
        }

        public static HttpContent ReadAsStreamContent(this IFileProvider fileProvider, string subpath, Action<HttpContentHeaders> headersCofigurer = null, string mediaType = StreamMediaType)
        {
            return fileProvider.ReadAsHttpContent(subpath, headersCofigurer, mediaType);
        }

        public static HttpContent ReadAsStringContent(this IFileProvider fileProvider, string subpath, Action<HttpContentHeaders> headersCofigurer = null, string mediaType = JsonMediaType)
        {
            return fileProvider.ReadAsHttpContent(subpath, headersCofigurer, mediaType);
        }

        public static HttpContent ReadAsHttpContent(this IFileProvider fileProvider, string subpath, Action<HttpContentHeaders> headersCofigurer = null, string mediaType = JsonMediaType)
        {
            var httpContent = string.IsNullOrWhiteSpace(mediaType) || mediaType.Equals(StreamMediaType)
                ? (HttpContent)new StreamContent(fileProvider.ReadAsStream(subpath))
                : (HttpContent)new StringContent(fileProvider.ReadAsString(subpath), Encoding.UTF8, mediaType);
            headersCofigurer?.Invoke(httpContent.Headers);
            return httpContent;
        }

        public static HttpContent CreateHttpContent<T>(this IFileProvider fileProvider, T value, Action<HttpContentHeaders> headersCofigurer = null, string mediaType = JsonMediaType)
        {
            var content = value is string ? value.ToString() : JsonConvert.SerializeObject(value, SerializerSettings);
            var httpContent = string.IsNullOrWhiteSpace(mediaType) || mediaType.Equals(StreamMediaType)
                ? (HttpContent)new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content)))
                : (HttpContent)new StringContent(content, Encoding.UTF8, mediaType);
            headersCofigurer?.Invoke(httpContent.Headers);
            return httpContent;
        }
    }
}
using System;
using System.IO;
using System.Reflection;
using System.Text;

public class ResourceHelper
{
    public static Stream GetResourceStream(string resourcePath, Assembly assembly = null)
    {
        if (string.IsNullOrEmpty(resourcePath)) throw new ArgumentNullException(nameof(resourcePath));
        var resourceName = resourcePath.Replace('/', '.');
#if NETSTANDARD1_0
    assembly = typeof(ResourceHelper).GetTypeInfo().Assembly;
#else
        assembly = assembly ?? typeof(ResourceHelper).Assembly;
#endif
        return assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{resourceName}");
    }

    public static string GetResourceString(string resourceName, Assembly assembly = null)
    {
        var stream = GetResourceStream(resourceName, assembly);
        if (stream == null) return string.Empty;
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }
}

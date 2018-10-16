using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace GSS.Authentication.CAS.Testing
{
    public class CasFixture
    {
        public CasFixture()
        {
            Service = "http://localhost";
            Options = new CasOptions
            {
                CasServerUrlBase = "http://example.com/cas"
            };
        }

        public string Service { get; }
        public ICasOptions Options { get; }
        public IFileProvider FileProvider => new EmbeddedFileProvider(typeof(CasFixture).GetTypeInfo().Assembly, typeof(CasFixture).Namespace + ".Resources");
    }
}

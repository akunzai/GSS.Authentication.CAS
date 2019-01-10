using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(AspNetCoreIdentitySample.Areas.Identity.IdentityHostingStartup))]
namespace AspNetCoreIdentitySample.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
            });
        }
    }
}
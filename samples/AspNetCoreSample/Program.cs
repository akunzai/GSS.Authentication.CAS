using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;

namespace AspNetCoreSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .ConfigureLogging((context, logging) =>
                        {
                            // configure nlog.config per environment
                            var configFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, $"NLog.{context.HostingEnvironment.EnvironmentName}.config"));
                            NLog.Web.NLogBuilder.ConfigureNLog(configFile.Exists ? configFile.Name : "NLog.config");

                            logging.AddNLog();
                        });
                });
    }
}

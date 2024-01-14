using System;
using GSS.Authentication.CAS.Owin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Security.Cookies;

namespace OwinSample.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSingleLogout(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddDistributedMemoryCache();
            var redisConfiguration = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConfiguration))
            {
                services.AddStackExchangeRedisCache(options => options.Configuration = redisConfiguration);
            }

            services
                .AddSingleton(configuration)
                .AddSingleton<IAuthenticationSessionStore, DistributedCacheIAuthenticationSessionStore>()
                .BuildServiceProvider();

            return services;
        }
    }
}
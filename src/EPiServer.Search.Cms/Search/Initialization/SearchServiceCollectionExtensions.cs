using System;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EPiServer.Search.Initialization
{
    public static class SearchServiceCollectionExtensions
    {
        public static IServiceCollection AddBasicSearch(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton(configuration);

            return services;
        }
    }
}

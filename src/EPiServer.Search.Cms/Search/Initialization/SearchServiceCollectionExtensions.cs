using EPiServer.Search.Configuration;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPiServer.Search.Cms.Search.Initialization
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

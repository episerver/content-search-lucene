using EPiServer.Framework;
using EPiServer.Search.IndexingService.Configuration;
using EPiServer.Search.IndexingService.Helpers;
using EPiServer.Search.IndexingService.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> related to EPiServer.Search.IndexingService
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register services EPiServer.Search.IndexingService
        /// </summary>
        public static IServiceCollection AddSearchIndexingService(this IServiceCollection services, IConfiguration serviceConfiguration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            //register
            services.AddSingleton<IIndexingServiceSettings, IndexingServiceSettings>();
            services.AddSingleton<IIndexingServiceHandler, IndexingServiceHandler>();
            services.AddSingleton<IResponseExceptionHelper, ResponseExceptionHelper>();
            services.AddSingleton<ICommonFunc, CommonFunc>();
            services.AddSingleton<IFeedHelper, FeedHelper>();
            services.AddSingleton<IDocumentHelper, DocumentHelper>();
            services.AddSingleton<ILuceneHelper, LuceneHelper>();
            services.AddSingleton<ClientElementHandler>();

            //register configuration
            services.Configure<IndexingServiceOptions>(serviceConfiguration);
            services.AddOptions<EpiserverFrameworkOptions>().PostConfigure<IHostEnvironment>((options, hosting) =>
            {
                if (options.AppDataPath == null)
                {
                    options.AppDataPath = Path.Combine(hosting.ContentRootPath, EnvironmentOptions.DefaultAppDataFolderName);
                }
            });

            services.AddControllers(options =>
                options.Filters.Add(new HttpResponseExceptionFilter()));

            services.AddHttpContextAccessor();
            services.AddSingleton<ISecurityHandler, SecurityHandler>();

            return services;
        }
    }
}
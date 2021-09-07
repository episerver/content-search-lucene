using EPiServer.Framework;
using EPiServer.Search.IndexingService.Configuration;
using EPiServer.Search.IndexingService.Helpers;
using EPiServer.Search.IndexingService.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //register
            services.AddSingleton<IIndexingServiceSettings, IndexingServiceSettings>();
            services.AddSingleton<IIndexingServiceHandler, IndexingServiceHandler>();
            services.AddSingleton<IResponseExceptionHelper, ResponseExceptionHelper>();
            services.AddSingleton<ICommonFunc, CommonFunc>();
            services.AddSingleton<IFeedHelper, FeedHelper>();
            services.AddSingleton<ILuceneHelper, LuceneHelper>();
            services.AddSingleton<ClientElementHandler>();

            //register configuration
            services.Configure<IndexingServiceOptions>(Configuration.GetSection("EPiServer:episerver.search.indexingservice"));
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
            services.AddSingleton<SecurityHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

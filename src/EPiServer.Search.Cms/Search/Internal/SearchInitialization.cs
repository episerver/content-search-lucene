using System;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging;
using EPiServer.ServiceLocation;

namespace EPiServer.Search.Internal
{
    // <internal-api />
    [InitializableModule]
    public class SearchInitialization : IConfigurableModule
    {
        private static ILogger _log = LogManager.GetLogger();

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<SearchHandler>();
            context.Services.AddSingleton<RequestHandler>();
            context.Services.AddSingleton<RequestQueue>();
            context.Services.AddSingleton<RequestQueueHandler>();
            context.Services.AddSingleton<ReIndexManager>();
            context.Services.Forward<ReIndexManager, IReIndexManager>();
        }

        public void Initialize(InitializationEngine context)
        {
            var searchOptions = context.Locate.Advanced.GetInstance<SearchOptions>();
            SearchSettings.Options = searchOptions;
            if (!searchOptions.Active)
            {
                return;
            }

            //Load search result filter providers
            SearchSettings.LoadSearchResultFilterProviders(searchOptions, context.Locate.Advanced);

            // Provoke a certificate error if user configured an invalid certificate
            foreach (var serviceReference in searchOptions.IndexingServiceReferences)
            {
                if (!serviceReference.BaseUri.IsWellFormedOriginalString())
                {
                    throw new ArgumentException($"The Base uri is not well formed '{serviceReference.BaseUri}'");
                }
                serviceReference.GetClientCertificate();
            }

            // Avoid starting the Queue flush timer during installation, since it risks breaking appdomain unloading
            // (it may stall in unmanaged code (socket/http request) causing an UnloadAppDomainException)
            if (context == null || context.HostType != HostType.Installer)
            {
                var queueHandler = context.Locate.Advanced.GetInstance<RequestQueueHandler>();
                queueHandler.StartQueueFlushTimer();
            }
            else
            {
                _log.Information("Didn't start the Queue Flush timer, since HostType is 'Installer'");
            }

            //Fire event telling that the default configuration is loaded
            SearchSettings.OnInitializationCompleted();

            _log.Information("Search Module Started!");
        }

        public void Uninitialize(InitializationEngine context)
        {
            SearchSettings.SearchResultFilterProviders.Clear();
            _log.Information("Search Module Stopped!");
        }
    }
}

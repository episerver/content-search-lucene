using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;
using EPiServer.Data;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging;
using EPiServer.Search.Configuration;
using EPiServer.Search.Configuration.Transform.Internal;
using EPiServer.Search.Internal;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EPiServer.Search.Initialization
{
    /// <summary>
    /// When this module is initialized the EPiServer Search runtime is initialized.
    /// </summary>
    [InitializableModule]
    [ModuleDependency(typeof(Web.InitializationModule))]
    public class SearchInitialization : IConfigurableModule
    {
        private SearchEventHandler _eventHandler;
        private static readonly object _lock = new object();
        private static readonly ILogger _log = LogManager.GetLogger();

        /// <inherit-doc/>
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            var serviceProvider = context.Services.BuildServiceProvider();
            var configuration = serviceProvider.GetService<IConfiguration>();
            var searchConfiguration = new SearchConfiguration();
            configuration.GetSection("EPiServer:episerver.search").Bind(searchConfiguration);

            context.Services
                .Configure<SearchOptions>(searchOptions => SearchOptionsTransform.Transform(searchConfiguration, searchOptions))
                .AddSingleton<SearchHandler>()
                .AddSingleton<RequestHandler>()
                .AddSingleton<RequestQueue>()
                .AddSingleton<RequestQueueHandler>()
                .AddSingleton<ReIndexManager>()
                .Forward<ReIndexManager, IReIndexManager>()
                .Configure<ProtectedModuleOptions>(o =>
                 {
                     if (!o.Items.Any(x => x.Name.Equals("EPiServer.Search.Cms")))
                     {
                         o.Items.Add(new ModuleDetails() { Name = "EPiServer.Search.Cms" });
                     }
                 });
        }

        /// <inherit-doc/>
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

            // TO BE UPDATED: investigate why old code need to check installer
            if (context == null)
            {
                _log.Information("Didn't start the Queue Flush timer, since context is null");
            }
            else
            {
                var queueHandler = context.Locate.Advanced.GetInstance<RequestQueueHandler>();
                queueHandler.StartQueueFlushTimer();
            }

            //Fire event telling that the default configuration is loaded
            SearchSettings.OnInitializationCompleted();

            if (context.Locate.Advanced.GetInstance<IDatabaseMode>().DatabaseMode == DatabaseMode.ReadOnly)
            {
                _log.Debug("Unable to indexing of content because the database is in the ReadOnly mode");
            }
        }

        /// <inherit-doc/>
        public void Uninitialize(InitializationEngine context)
        {
            if (_eventHandler != null)
            {
                var contentEvents = context.Locate.ContentEvents();
                var contentSecurityRepo = context.Locate.ContentSecurityRepository();
                var contentSecurityEvents = context.Locate.ContentSecurityEvents();
                contentEvents.PublishedContent -= _eventHandler.ContentEvents_PublishedContent;
                contentEvents.MovedContent -= _eventHandler.ContentEvents_MovedContent;
                contentEvents.DeletingContent -= _eventHandler.ContentEvents_DeletingContent;
                contentEvents.DeletedContent -= _eventHandler.ContentEvents_DeletedContent;
                contentEvents.DeletedContentLanguage -= _eventHandler.ContentEvents_DeletedContentLanguage;

                contentSecurityEvents.ContentSecuritySaved -= _eventHandler.ContentSecurityRepository_Saved;

                PageTypeConverter.PagesConverted -= _eventHandler.PageTypeConverter_PagesConverted;

                _eventHandler = null;
            }

            SearchSettings.SearchResultFilterProviders.Clear();
        }

        #region Event handlers

        public class SearchEventHandler
        {
            private const string DeletedVirtualPathNodes = "DeletedVirtualPathNodes";

            private readonly ContentSearchHandler _contentSearchHandler;
            private readonly IContentRepository _contentRepository;

            public SearchEventHandler(ContentSearchHandler contentSearchHandler, IContentRepository contentRepository)
            {
                _contentSearchHandler = contentSearchHandler;
                _contentRepository = contentRepository;
            }

            public void ContentEvents_PublishedContent(object sender, ContentEventArgs e) => _contentSearchHandler.UpdateItem(e.Content);

            public void ContentEvents_MovedContent(object sender, ContentEventArgs e) => _contentSearchHandler.MoveItem((e as MoveContentEventArgs).OriginalContentLink);

            public void ContentEvents_DeletingContent(object sender, ContentEventArgs e)
            {
                if (!ContentReference.IsNullOrEmpty(e.ContentLink))
                {
                    e.Items.Add(DeletedVirtualPathNodes, _contentSearchHandler.GetVirtualPathNodes(e.ContentLink));
                }
            }

            public void ContentEvents_DeletedContent(object sender, ContentEventArgs e)
            {
                var virtualPathNodes = e.Items[DeletedVirtualPathNodes] as ICollection<string>;
                if (virtualPathNodes != null)
                {
                    _contentSearchHandler.RemoveItemsByVirtualPath(virtualPathNodes);
                }
            }

            public void ContentEvents_DeletedContentLanguage(object sender, ContentEventArgs e) => _contentSearchHandler.RemoveLanguageBranch(e.Content);

            public void ContentSecurityRepository_Saved(object sender, ContentSecurityEventArg e)
            {
                var contents = _contentRepository.GetLanguageBranches<IContent>(e.ContentLink);
                foreach (var c in contents)
                {
                    var versionableContent = c as IVersionable;
                    if (versionableContent == null || (!versionableContent.IsPendingPublish))
                    {
                        _contentSearchHandler.UpdateItem(c);
                    }
                }
            }

            public void PageTypeConverter_PagesConverted(object sender, ConvertedPageEventArgs e)
            {
                if (_contentRepository.TryGet(e.PageLink, out IContent content))
                {
                    _contentSearchHandler.UpdateItem(content);
                }

                if (e.Recursive)
                {
                    UpdateConvertedChildren(e.PageLink, e.ToPageType.ID);
                }
            }

            private void UpdateConvertedChildren(ContentReference parent, int contentTypeID)
            {
                foreach (var child in _contentRepository.GetChildren<IContent>(parent))
                {
                    if (child.ContentTypeID == contentTypeID)
                    {
                        _contentSearchHandler.UpdateItem(child);
                    }

                    UpdateConvertedChildren(child.ContentLink, contentTypeID);
                }
            }
        }

        #endregion

    }
}

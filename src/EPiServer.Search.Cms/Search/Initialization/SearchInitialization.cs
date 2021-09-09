using EPiServer.Configuration.Transform.Internal;
using EPiServer.Core;
using EPiServer.Data;
using EPiServer.Data.Dynamic;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging;
using EPiServer.Search.Internal;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using HostType = EPiServer.Framework.Initialization.HostType;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using EPiServer.Search.Configuration;
using EPiServer.Search.Configuration.Transform.Internal;

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
        private static object _lock = new object();
        private static readonly ILogger _log = LogManager.GetLogger();

        /// <inherit-doc/>
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services
                .AddSingleton<SearchHandler>()
                .AddSingleton<RequestHandler>()
                .AddSingleton<RequestQueue>()
                .AddSingleton<RequestQueueHandler>()
                .AddSingleton<ReIndexManager>()
                .Forward<ReIndexManager, IReIndexManager>();
        }

        /// <inherit-doc/>
        public void Initialize(InitializationEngine context)
        {
            var configuration = ServiceLocator.Current.GetInstance<IConfiguration>();
            var searchConfiguration = new SearchConfiguration();
            configuration.GetSection("EPiServer:episerver.search").Bind(searchConfiguration);

            var searchOptions = new SearchOptions();
            SearchOptionsTransform.Transform(searchConfiguration, searchOptions);
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
            if (context == null)
            {
                var queueHandler = context.Locate.Advanced.GetInstance<RequestQueueHandler>();
                queueHandler.StartQueueFlushTimer();
            }
            else
            {
                _log.Information("Didn't start the Queue Flush timer, since context is null");
            }

            //Fire event telling that the default configuration is loaded
            SearchSettings.OnInitializationCompleted();

            if (context.Locate.Advanced.GetInstance<IDatabaseMode>().DatabaseMode == DatabaseMode.ReadOnly)
            {
                _log.Debug("Unable to indexing of content because the database is in the ReadOnly mode");
                return;
            }

            if (SearchSettings.Options.Active && (context.HostType == HostType.WebApplication || context.HostType == HostType.LegacyMirroringAppDomain))
            {
                var contentRepo = context.Locate.ContentRepository();
                var contentSecurityRepo = context.Locate.ContentSecurityRepository();
                var contentEvents = context.Locate.ContentEvents();

                ContentSearchHandler contentSearchHandler = context.Locate.Advanced.GetInstance<ContentSearchHandler>();
                _eventHandler = new SearchEventHandler(contentSearchHandler, contentRepo);

                contentEvents.PublishedContent += _eventHandler.ContentEvents_PublishedContent;
                contentEvents.MovedContent += _eventHandler.ContentEvents_MovedContent;
                contentEvents.DeletingContent += _eventHandler.ContentEvents_DeletingContent;
                contentEvents.DeletedContent += _eventHandler.ContentEvents_DeletedContent;
                contentEvents.DeletedContentLanguage += _eventHandler.ContentEvents_DeletedContentLanguage;

                contentSecurityRepo.ContentSecuritySaved += _eventHandler.ContentSecurityRepository_Saved;

                PageTypeConverter.PagesConverted += _eventHandler.PageTypeConverter_PagesConverted;

                RequestQueueRemover requestQueueRemover = new RequestQueueRemover(context.Locate.Advanced.GetInstance<SearchHandler>());
                ThreadPool.QueueUserWorkItem(new WaitCallback(state => { IndexAllOnce(contentSearchHandler, requestQueueRemover); }));
            }
        }

        /// <inherit-doc/>
        public void Uninitialize(InitializationEngine context)
        {
            if (_eventHandler != null)
            {
                var contentEvents = context.Locate.ContentEvents();
                var contentSecurityRepo = context.Locate.ContentSecurityRepository();

                contentEvents.PublishedContent -= _eventHandler.ContentEvents_PublishedContent;
                contentEvents.MovedContent -= _eventHandler.ContentEvents_MovedContent;
                contentEvents.DeletingContent -= _eventHandler.ContentEvents_DeletingContent;
                contentEvents.DeletedContent -= _eventHandler.ContentEvents_DeletedContent;
                contentEvents.DeletedContentLanguage -= _eventHandler.ContentEvents_DeletedContentLanguage;

                contentSecurityRepo.ContentSecuritySaved -= _eventHandler.ContentSecurityRepository_Saved;

                PageTypeConverter.PagesConverted -= _eventHandler.PageTypeConverter_PagesConverted;

                _eventHandler = null;
            }

            SearchSettings.SearchResultFilterProviders.Clear();
        }

        private void IndexAllOnce(ContentSearchHandler contentSearchHandler, RequestQueueRemover requestQueueRemover)
        {
            lock (_lock)
            {
                try
                {
                    if (HasIndexingBeenExecuted())
                    {
                        return;
                    }

                    requestQueueRemover.TruncateQueue(new IReIndexable[] { contentSearchHandler });
                    contentSearchHandler.IndexPublishedContent();

                    Storage.Save(new IndexingInformation() { ExecutionDate = DateTime.Now });
                }
                catch (Exception e)
                {
                    // We do not want any type of exception left unhandled since we are running on a separete thread.
                    _log.Warning("Error during full search indexing of content and files.", e);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the indexing has been executed before.
        /// </summary>
        /// <value><c>true</c> if indexing was done; otherwise, <c>false</c>.</value>
        private bool HasIndexingBeenExecuted()
        {
            IndexingInformation execution = Storage.LoadAll<IndexingInformation>().FirstOrDefault();

            if (execution != null && execution.ExecutionDate > DateTime.MinValue)
            {
                return true;
            }

            return false;
        }

        private DynamicDataStore Storage
        {
            get
            {
                return DynamicDataStoreFactory.Instance.GetStore(typeof(IndexingInformation)) ??
                    DynamicDataStoreFactory.Instance.CreateStore(typeof(IndexingInformation));
            }
        }

        #region Event handlers

        internal class SearchEventHandler
        {
            private const string DeletedVirtualPathNodes = "DeletedVirtualPathNodes";

            private readonly ContentSearchHandler _contentSearchHandler;
            private readonly IContentRepository _contentRepository;

            public SearchEventHandler(ContentSearchHandler contentSearchHandler, IContentRepository contentRepository)
            {
                _contentSearchHandler = contentSearchHandler;
                _contentRepository = contentRepository;
            }

            public void ContentEvents_PublishedContent(object sender, ContentEventArgs e)
            {
                _contentSearchHandler.UpdateItem(e.Content);
            }

            public void ContentEvents_MovedContent(object sender, ContentEventArgs e)
            {
                _contentSearchHandler.MoveItem((e as MoveContentEventArgs).OriginalContentLink);
            }

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

            public void ContentEvents_DeletedContentLanguage(object sender, ContentEventArgs e)
            {
                _contentSearchHandler.RemoveLanguageBranch(e.Content);
            }

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

        private class RequestQueueRemover
        {
            private static readonly ILogger _log = LogManager.GetLogger();
            private readonly SearchHandler _searchHandler;

            public RequestQueueRemover(SearchHandler searchHandler)
            {
                _searchHandler = searchHandler;
            }

            public void TruncateQueue(IReIndexable[] reIndexables)
            {
                foreach (IReIndexable reIndexable in reIndexables)
                {
                    _log.Information("The RequestQueueRemover truncate queue with NamedIndexingService '{0}' and NamedIndex '{1}'", reIndexable.NamedIndexingService, reIndexable.NamedIndex);
                    _searchHandler.TruncateQueue(reIndexable.NamedIndexingService, reIndexable.NamedIndex);
                }
            }
        }
    }
}

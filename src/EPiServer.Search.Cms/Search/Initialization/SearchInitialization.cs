using EPiServer.Core;
using EPiServer.Data;
using EPiServer.Data.Dynamic;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging.Compatibility;
using EPiServer.Search.Configuration;
using EPiServer.Search.Configuration.Transform.Internal;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HostType = EPiServer.Framework.Initialization.HostType;

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
        private static readonly ILog _log = LogManager.GetLogger(typeof(SearchInitialization));

        /// <inherit-doc/>
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<EPiServer.Configuration.Transform.Internal.IConfigurationTransform>(s =>
                new SearchOptionsTransform(s.GetInstance<SearchOptions>(), SearchSection.Instance));
        }

        /// <inherit-doc/>
        public void Initialize(InitializationEngine context)
        {
            if (context.Locate.Advanced.GetInstance<IDatabaseMode>().DatabaseMode ==  DatabaseMode.ReadOnly)
            {
                _log.Debug("Unable to indexing of content because the database is in the ReadOnly mode");
                return;
            }

            if (SearchSettings.Options.Active && (context.HostType == HostType.WebApplication || context.HostType == HostType.Service))
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

                _eventHandler = null;
            }
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
                    _log.Warn("Error during full search indexing of content and files.", e);
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
        }

        #endregion

        private class RequestQueueRemover
        {
            private static readonly ILog _log = LogManager.GetLogger(typeof(RequestQueueRemover));
            private readonly SearchHandler _searchHandler;

            public RequestQueueRemover(SearchHandler searchHandler)
            {
                _searchHandler = searchHandler;
            }

            public void TruncateQueue(IReIndexable[] reIndexables)
            {
                foreach (IReIndexable reIndexable in reIndexables)
                {
                    if (_log.IsInfoEnabled)
                    {
                        _log.InfoFormat("The RequestQueueRemover truncate queue with NamedIndexingService '{0}' and NamedIndex '{1}'", reIndexable.NamedIndexingService, reIndexable.NamedIndex);
                    }
                    _searchHandler.TruncateQueue(reIndexable.NamedIndexingService, reIndexable.NamedIndex);
                }
            }
        }
    }
}

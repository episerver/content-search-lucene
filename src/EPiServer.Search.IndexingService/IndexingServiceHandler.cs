using EPiServer.Search.IndexingService.Controllers;
using EPiServer.Search.IndexingService.FieldSerializers;
using EPiServer.Search.IndexingService.Helpers;
using EPiServer.Search.IndexingService.Models;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace EPiServer.Search.IndexingService
{
    /// <summary>
    /// Class responsible for handling requests to the indexing service
    /// </summary>
    public class IndexingServiceHandler : IIndexingServiceHandler
    {
        #region members
        private static TaskQueue _taskQueue = null;

        private readonly IFeedHelper _feedHelper;
        private readonly ILuceneHelper _luceneHelper;
        private readonly ICommonFunc _commonFunc;
        private readonly IResponseExceptionHelper _responseExceptionHelper;
        private readonly IDocumentHelper _documentHelper;
        #endregion

        #region Constructors
        public IndexingServiceHandler(IFeedHelper feedHelper,
            ILuceneHelper luceneHelper,
            ICommonFunc commonFunc,
            IResponseExceptionHelper responseExceptionHelper,
            IDocumentHelper documentHelper)
        {
            if(_taskQueue == null)
            {
                _taskQueue = new TaskQueue("indexing service data uri callback", 1000, TimeSpan.FromSeconds(0));
            }

            _feedHelper = feedHelper;
            _luceneHelper = luceneHelper;
            _commonFunc = commonFunc;
            _responseExceptionHelper = responseExceptionHelper;
            _documentHelper = documentHelper;
        }

        #endregion

        #region Protected Internal
        /// <summary>
        /// Updates the Lucene index from the passed syndication feed
        /// </summary>
        /// <param name="feed">The feed to process</param>
        public void UpdateIndex(FeedModel feed)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start processing feed '{0}'", feed.Id));

            foreach (FeedItemModel item in feed.Items)
            {
                string namedIndexName = _feedHelper.GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameNamedIndex);

                if (_commonFunc.IsValidIndex(namedIndexName) && _commonFunc.IsModifyIndex(namedIndexName))
                {
                    string referenceId = _feedHelper.GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameReferenceId);
                    string indexAction = _feedHelper.GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameIndexAction);
                    string dataUri = _feedHelper.GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameDataUri);

                    NamedIndex namedIndex = new NamedIndex(namedIndexName, !(String.IsNullOrEmpty(referenceId)));

                    // Set the named index to use. Check if an update or remove is commenced with a reference item.
                    if (String.IsNullOrEmpty(referenceId) && (indexAction == "update" || indexAction == "remove"))
                    {
                        referenceId = _luceneHelper.GetReferenceIdForItem(item.Id, namedIndex);
                        if (!String.IsNullOrEmpty(referenceId))
                        {
                            // Set the referenceId to the current item
                            _feedHelper.SetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameReferenceId, referenceId);

                            //Force usage of the reference index
                            namedIndex = new NamedIndex(namedIndexName, true);
                        }
                    }

                    IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start processing feed item '{0}' for '{1}'", item.Id, indexAction));

                    // If there is a callback uri defined, we run the callback in async mode
                    if (!String.IsNullOrEmpty(dataUri))
                    {
                        Action callback = new Action(new DataUriQueueItem(item, namedIndex, _luceneHelper).Do);
                        _taskQueue.Enqueue(callback);

                        IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Callback for data uri '{0}' enqueued", dataUri));
                    }
                    else
                    {
                        // If no callback data uri is defined, we handle the item in the current request thread
                        switch (indexAction)
                        {
                            case "add":
                                _luceneHelper.Add(item, namedIndex);
                                break;
                            case "update":
                                _luceneHelper.Update(item, namedIndex);
                                break;
                            case "remove":
                                _luceneHelper.Remove(item, namedIndex);
                                break;
                        }

                        // If this item is a reference item we need to update the parent document to 
                        // reflect changes in the reference index. e.g. comments.
                        if (!String.IsNullOrEmpty(referenceId))
                        {
                            _luceneHelper.UpdateReference(
                                referenceId,
                                item.Id,
                                new NamedIndex(namedIndexName)); // Always main index

                            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Updated reference with reference id '{0}' ", referenceId));
                        }
                    }
                }

                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End processing feed item '{0}'", item.Id));
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End processing feed '{0}'", feed.Id));
        }

        /// <summary>
        /// Gets <see cref="SearchResults"/> by query expression q using a query parser (user entered query)
        /// </summary>
        /// <param name="q">The query expression to be parsed. User entered</param>
        /// <param name="namedIndexeNames">A comma separatad string of named indexes to search in</param>
        /// <param name="offset">The offset from hit 1 to start collection hits from</param>
        /// <param name="limit">The number of items from offset to collect</param>
        /// <returns><see cref="SearchResults"/></return>s
        public FeedModel GetSearchResults(string q, string[] namedIndexNames, int offset, int limit)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start search with expression: '{0}'", q));

            int totalHits = 0;
            FeedModel feed = new FeedModel();
            Collection<FeedItemModel> feedItems = new Collection<FeedItemModel>();

            Collection<NamedIndex> namedIndexes = new Collection<NamedIndex>();
            if (namedIndexNames != null && namedIndexNames.Length > 0)
            {
                foreach (string index in namedIndexNames)
                {
                    NamedIndex namedIndex = new NamedIndex(index);
                    if (!namedIndex.IsValid)
                    {
                        _responseExceptionHelper.HandleServiceError(String.Format("Named index \"{0}\" is not valid, it does not exist in configuration or has faulty configuration", namedIndex.Name));
                        return null;
                    }
                    namedIndexes.Add(namedIndex);
                }
            }
            else
            {
                NamedIndex namedIndex = new NamedIndex();//Use default
                if (!namedIndex.IsValid)
                {
                    _responseExceptionHelper.HandleServiceError(String.Format("Named index \"{0}\" is not valid, it does not exist in configuration or has faulty configuration", namedIndex.Name));
                    return null;
                }
                namedIndexes.Add(namedIndex);
            }

            Collection<ScoreDocument> scoreDocuments = _luceneHelper.GetScoreDocuments(q, true, namedIndexes, offset, limit, IndexingServiceSettings.MaxHitsForSearchResults, out totalHits);

            int returnedHits = 0;
            foreach (ScoreDocument scoreDocument in scoreDocuments)
            {
                FeedItemModel feedItem = _documentHelper.GetSyndicationItemFromDocument(scoreDocument);
                feedItems.Add(feedItem);
                returnedHits++;
            }

            // Add total hits to feed
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationFeedAttributeNameTotalHits, totalHits.ToString(CultureInfo.InvariantCulture));

            // Add service version as an attribute extension to response feed
            System.Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string versionString = String.Format(CultureInfo.InvariantCulture, "EPiServer.Search v.{0}.{1}.{2}.{3}",
               version.Major.ToString(CultureInfo.InvariantCulture), version.Minor.ToString(CultureInfo.InvariantCulture),
               version.Build.ToString(CultureInfo.InvariantCulture), version.Revision.ToString(CultureInfo.InvariantCulture));
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationFeedAttributeNameVersion, versionString);

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End search with expression '{0}'. Returned {1} hits of total {2} with offset {3} and limit {4}",
                q, returnedHits.ToString(CultureInfo.InvariantCulture), totalHits.ToString(CultureInfo.InvariantCulture),
                offset.ToString(CultureInfo.InvariantCulture), limit.ToString(CultureInfo.InvariantCulture)));

            feed.Items = feedItems;

            return feed;
        }

        /// <summary>
        /// Gets the configured index names.
        /// </summary>
        /// <returns></returns>
        public FeedModel GetNamedIndexes()
        {
            FeedModel feed = new FeedModel();
            Collection<FeedItemModel> items = new Collection<FeedItemModel>();
            foreach (string name in IndexingServiceSettings.NamedIndexElements.Keys)
            {
                FeedItemModel item = new FeedItemModel();
                item.Title = name;
                items.Add(item);
            }
            feed.Items = items;
            return feed;
        }

        /// <summary>
        /// Wipes the supplied named index and creates a new one
        /// </summary>
        /// <param name="namedIndex">The named index to wipe</param>
        public void ResetNamedIndex(string namedIndexName)
        {
            NamedIndex namedIndex = new NamedIndex(namedIndexName);
            if (IndexingServiceSettings.NamedIndexElements.ContainsKey(namedIndexName))
            {
                _documentHelper.CreateIndex(namedIndex.Name, namedIndex.DirectoryInfo);
                _documentHelper.CreateIndex(namedIndex.ReferenceName, namedIndex.ReferenceDirectoryInfo);
            }
            else
            {
                _responseExceptionHelper.HandleServiceError(String.Format("Reset of index: '{0}' failed. Index not found!", namedIndexName));
            }
        }

        public FeedModel GetSearchResults(string q, string namedIndexes, int offset, int limit)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Request for search with query parser with expression: {0} in named indexes: {1}", q, namedIndexes));

            //Parse named indexes string from request
            string[] namedIndexesArr = null;
            if (!String.IsNullOrEmpty(namedIndexes))
            {
                char[] delimiter = { '|' };
                namedIndexesArr = namedIndexes.Split(delimiter);
            }

            return GetSearchResults(q, namedIndexesArr, offset, limit);
        }
        #endregion
    }
}

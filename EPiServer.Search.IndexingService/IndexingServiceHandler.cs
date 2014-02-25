
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using EPiServer.Search.IndexingService.FieldSerializers;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace EPiServer.Search.IndexingService
{
    /// <summary>
    /// Class responsible for handling requests to the indexing service
    /// </summary>
    public class IndexingServiceHandler
    {
        #region members

        /// <summary>
        /// Id that indicates that the item itself should be ignored - but side effects should be processed
        /// </summary>
        private const string IgnoreItemId = "<IgnoreItemId>";

        private static IndexingServiceHandler _instance = new IndexingServiceHandler();

        private static TaskQueue _taskQueue = new TaskQueue("indexing service data uri callback", 1000, TimeSpan.FromSeconds(0));

        #endregion

        #region Constructors

        protected IndexingServiceHandler()
        {
        }

        static IndexingServiceHandler()
        {
        }

        public static IndexingServiceHandler Instance
        {
            get
            {
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        #endregion

        #region Protected Internal

        /// <summary>
        /// Updates the Lucene index from the passed syndication feed
        /// </summary>
        /// <param name="feed">The feed to process</param>
        protected internal virtual void UpdateIndex(SyndicationFeed feed)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start processing feed '{0}'", feed.Id));

            foreach (SyndicationItem item in feed.Items)
            {
                string namedIndexName = GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameNamedIndex);

                if (IsValidIndex(namedIndexName) && IsModifyIndex(namedIndexName))
                {
                    string referenceId = GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameReferenceId);
                    string indexAction = GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameIndexAction);
                    string dataUri = GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameDataUri);

                    NamedIndex namedIndex = new NamedIndex(namedIndexName, !(String.IsNullOrEmpty(referenceId)));

                    // Set the named index to use. Check if an update or remove is commenced with a reference item.
                    if (String.IsNullOrEmpty(referenceId) && (indexAction == "update" || indexAction == "remove"))
                    {
                        referenceId = GetReferenceIdForItem(item.Id, namedIndex);
                        if (!String.IsNullOrEmpty(referenceId))
                        {
                            // Set the referenceId to the current item
                            SetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameReferenceId, referenceId);

                            //Force usage of the reference index
                            namedIndex = new NamedIndex(namedIndexName, true);
                        }
                    }
                    
                    IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start processing feed item '{0}' for '{1}'", item.Id, indexAction));

                    // If there is a callback uri defined, we run the callback in async mode
                    if (!String.IsNullOrEmpty(dataUri))
                    {
                        Action callback = new Action(new DataUriQueueItem(item, namedIndex).Do);
                        _taskQueue.Enqueue(callback);

                        IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Callback for data uri '{0}' enqueued", dataUri));
                    }
                    else
                    {
                        // If no callback data uri is defined, we handle the item in the current request thread
                        switch (indexAction)
                        {
                            case "add":
                                Add(item, namedIndex);
                                break;
                            case "update":
                                Update(item, namedIndex);
                                break;
                            case "remove":
                                Remove(item, namedIndex);
                                break;
                        }

                        // If this item is a reference item we need to update the parent document to 
                        // reflect changes in the reference index. e.g. comments.
                        if (!String.IsNullOrEmpty(referenceId))
                        {
                            UpdateReference(
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
        protected internal virtual SyndicationFeedFormatter GetSearchResults(string q, string[] namedIndexNames, int offset, int limit)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start search with expression: '{0}'", q));

            int totalHits = 0;
            SyndicationFeed feed = new SyndicationFeed();
            Collection<SyndicationItem> syndicationItems = new Collection<SyndicationItem>();

            Collection<NamedIndex> namedIndexes = new Collection<NamedIndex>();
            if (namedIndexNames != null && namedIndexNames.Length > 0)
            {
                foreach (string index in namedIndexNames)
                {
                    NamedIndex namedIndex = new NamedIndex(index);
                    if (!namedIndex.IsValid)
                    {
                        IndexingServiceSettings.HandleServiceError(String.Format("Named index \"{0}\" is not valid, it does not exist in configuration or has faulty configuration", namedIndex.Name));
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
                    IndexingServiceSettings.HandleServiceError(String.Format("Named index \"{0}\" is not valid, it does not exist in configuration or has faulty configuration", namedIndex.Name));
                    return null;
                }
                namedIndexes.Add(namedIndex); 
            }

            Collection<ScoreDocument> scoreDocuments = GetScoreDocuments(q, true, namedIndexes, offset, limit, IndexingServiceSettings.MaxHitsForSearchResults, out totalHits);

            int returnedHits = 0;
            foreach (ScoreDocument scoreDocument in scoreDocuments)
            {
                SyndicationItem syndicationItem = GetSyndicationItemFromDocument(scoreDocument);
                syndicationItems.Add(syndicationItem);
                returnedHits++;
            }

            // Add total hits to feed
            feed.AttributeExtensions.Add
                (new XmlQualifiedName(IndexingServiceSettings.SyndicationFeedAttributeNameTotalHits,
                    IndexingServiceSettings.XmlQualifiedNamespace), totalHits.ToString(CultureInfo.InvariantCulture));

            // Add service version as an attribute extension to response feed
            System.Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string versionString = String.Format(CultureInfo.InvariantCulture, "EPiServer.Search v.{0}.{1}.{2}.{3}",
               version.Major.ToString(CultureInfo.InvariantCulture), version.Minor.ToString(CultureInfo.InvariantCulture),
               version.Build.ToString(CultureInfo.InvariantCulture), version.Revision.ToString(CultureInfo.InvariantCulture));
            feed.AttributeExtensions.Add
               (new XmlQualifiedName(IndexingServiceSettings.SyndicationFeedAttributeNameVersion,
                   IndexingServiceSettings.XmlQualifiedNamespace), versionString);

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End search with expression '{0}'. Returned {1} hits of total {2} with offset {3} and limit {4}",
                q, returnedHits.ToString(CultureInfo.InvariantCulture), totalHits.ToString(CultureInfo.InvariantCulture),
                offset.ToString(CultureInfo.InvariantCulture), limit.ToString(CultureInfo.InvariantCulture)));

            feed.Items = syndicationItems;

            return new Atom10FeedFormatter(feed);
        }

        /// <summary>
        /// Gets the configured index names.
        /// </summary>
        /// <returns></returns>
        protected internal virtual SyndicationFeedFormatter GetNamedIndexes()
        {
            SyndicationFeed feed = new SyndicationFeed();
            Collection<SyndicationItem> items = new Collection<SyndicationItem>();
            foreach (string name in IndexingServiceSettings.NamedIndexElements.Keys)
            {
                SyndicationItem item = new SyndicationItem();
                item.Title = new TextSyndicationContent(name);
                items.Add(item);
            }
            feed.Items = items;
            return new Atom10FeedFormatter(feed);
        }

        /// <summary>
        /// Wipes the supplied named index and creates a new one
        /// </summary>
        /// <param name="namedIndex">The named index to wipe</param>
        protected internal virtual void ResetNamedIndex(string namedIndexName)
        {
            NamedIndex namedIndex = new NamedIndex(namedIndexName);
            if (IndexingServiceSettings.NamedIndexElements.ContainsKey(namedIndexName))
            {
                CreateIndex(namedIndex.Name, namedIndex.DirectoryInfo);
                CreateIndex(namedIndex.ReferenceName, namedIndex.ReferenceDirectoryInfo);
            }
            else
            {
                IndexingServiceSettings.HandleServiceError(String.Format("Reset of index: '{0}' failed. Index not found!", namedIndexName));
            }
        }
        
        /// <summary>
        /// Gets the text content for the passed uri that is not a file uri. Not implemented. Should be overridden.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> to get content from</param>
        /// <returns>Empty string</returns>
        protected internal virtual string GetNonFileUriContent(Uri uri)
        {
            return "";
        }

        /// <summary>
        /// Gets the text content for the passed file uri using the uri.LocalPath
        /// </summary>
        /// <param name="uri">The file <see cref="Uri"/> to get content from</param>
        /// <returns></returns>     
        protected internal virtual string GetFileUriContent(Uri uri)
        {
            return GetFileText(uri.LocalPath);
        }

        #endregion

        #region Internal

        /// <summary>
        /// Runs in async mode and handles when a DataUri is defined and the content should be grabbed from the uri rather than the item itself
        /// </summary>
        /// <param name="item">The <see cref="SyndicationItem"/> passed to the index</param>
        /// <param name="namedIndex">The <see cref="NamedIndex"/> to use</param>
        internal void HandleDataUri(SyndicationItem item, NamedIndex namedIndex)
        {
            // Get the uri string
            string uriString = GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameDataUri);

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start processing data uri callback for uri '{0}'", uriString));

            Uri uri = null;

            // Try to parse the uri
            if (!Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out uri))
            {
                IndexingServiceSettings.HandleServiceError(String.Format("Data Uri callback failed. Uri '{0}' is not well formed", uriString));     
                return;
            }

            string content = String.Empty;

            if (uri.IsFile)
            {
                if (!File.Exists(uri.LocalPath))
                {
                    IndexingServiceSettings.HandleServiceError(String.Format("File for uri '{0}' does not exist", uri.ToString()));
                    return;
                }

                content = GetFileUriContent(uri);
            }
            else
            {
                content = GetNonFileUriContent(uri);
            }

            if (String.IsNullOrEmpty(content))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Content for uri '{0}' is empty", uri.ToString()));
                content = String.Empty;
            }

            string displayTextOut = String.Empty;
            string metadataOut = String.Empty;

            TextSyndicationContent textContent = item.Content as TextSyndicationContent;
            if (textContent != null && !String.IsNullOrEmpty(textContent.Text))
            {
                SplitDisplayTextToMetadata(textContent.Text + " " + content ,
                    GetElementValue(item, IndexingServiceSettings.SyndicationItemElementNameMetadata),
                                    out displayTextOut, out metadataOut);
            }
            else
            {
                SplitDisplayTextToMetadata(content,
                    GetElementValue(item, IndexingServiceSettings.SyndicationItemElementNameMetadata),
                                    out displayTextOut, out metadataOut);
            }

            item.Content = new TextSyndicationContent(displayTextOut);
            SetElementValue(item, IndexingServiceSettings.SyndicationItemElementNameMetadata, metadataOut);
            
            string requestType = GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameIndexAction);
            if (requestType == "add")
            {
                Add(item, namedIndex);
            }
            else if (requestType == "update")
            {
                Update(item, namedIndex);
            }

            string referenceId = GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameReferenceId);

            // If this item is a reference item we need to update the parent document to 
            // reflect changes in the reference index. e.g. comments.
            if (!String.IsNullOrEmpty(referenceId))
            {
                UpdateReference(
                    referenceId,
                    item.Id,
                    new NamedIndex(namedIndex.Name)); //Always main index

                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Updated reference with reference id '{0}' ", referenceId));
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End data uri callback"));
        }

        internal static Lucene.Net.Store.Directory CreateIndex(string name, DirectoryInfo directoryInfo)
        {
            Lucene.Net.Store.Directory dir = null;

            IndexingServiceSettings.ReaderWriterLocks[name].EnterWriteLock();

            try
            {
                //Create directory
                dir = FSDirectory.Open(directoryInfo);
                //Create index
                using (IndexWriter iWriter = new IndexWriter(dir, new StandardAnalyzer(IndexingServiceSettings.LuceneVersion), true, Lucene.Net.Index.IndexWriter.MaxFieldLength.UNLIMITED))
                {
                }
            }
            catch (Exception e)
            {
                IndexingServiceSettings.HandleServiceError(String.Format("Failed to create index for path: '{0}'. Message: {1}{2}'", directoryInfo.FullName, e.Message, e.StackTrace));
            }
            finally
            {
                IndexingServiceSettings.ReaderWriterLocks[name].ExitWriteLock();
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Created index for path: '{0}'", directoryInfo.FullName));

            return dir;
        }

        #endregion

        #region private

        /// <summary>
        /// Search in the named index reference index for the itemId and returns the reference id for tha found item.
        /// This method is used to find out if the current item is a reference item when updating and removing the item
        /// </summary>
        /// <param name="itemId">The id of the item to search for</param>
        /// <param name="namedIndex">The Main index for which Reference index to search</param>
        /// <returns></returns>
        private string GetReferenceIdForItem(string itemId, NamedIndex namedIndex)
        {
            // Contruct the reference named index for the passed named index
            NamedIndex refNamedIndex = new NamedIndex(namedIndex.Name, true);

            Document doc = GetDocumentById(itemId, refNamedIndex);
            if (doc == null)
            {
                return null;
            }

            return doc.Get(IndexingServiceSettings.ReferenceIdFieldName); 
        }

        private Collection<ScoreDocument> GetScoreDocuments(string q, bool excludeNotPublished, Collection<NamedIndex> namedIndexes, int offset, int limit, int maxHits, out int totalHits)
        {
            Collection<ScoreDocument> scoreDocuments = new Collection<ScoreDocument>();

            //Handle Categories and ACL in expression
            q = PrepareExpression(q, excludeNotPublished);

            if (namedIndexes == null)
            {
                throw new ArgumentNullException("namedIndexes");
            }
            if (namedIndexes.Count == 0)
            {
                throw new ArgumentException("Called GetScoreDocuments without any named indexes", "namedIndexes");
            }

            if (namedIndexes.Count == 1)
            {
                scoreDocuments = SingleIndexSearch(q, namedIndexes[0], maxHits, out totalHits);
            }
            else
            {
                scoreDocuments = MultiIndexSearch(q, namedIndexes, maxHits, out totalHits);
            }

            Collection<ScoreDocument> results = new Collection<ScoreDocument>();
            int hitsToTake = limit + offset;
            hitsToTake = (totalHits < hitsToTake ? totalHits : hitsToTake);
            for (int i = offset; i < hitsToTake; i++)
            {
                results.Add(scoreDocuments[i]);
            }

            return results;
        }

        private void Add(SyndicationItem syndicationItem, NamedIndex namedIndex)
        {
            if (syndicationItem == null)
                return;

            string id = syndicationItem.Id;

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start adding Lucene document with id field: '{0}' to index: '{1}'", id, namedIndex.Name));

            if (String.IsNullOrEmpty(id))
            {
                IndexingServiceSettings.HandleServiceError(String.Format("Failed to add Document. id field is missing."));
                return;
            }

            //Check if the document already exists.
            if (DocumentExists(id, namedIndex))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Document already exists. Skipping."));
                return;
            }

            Document doc = GetDocumentFromSyndicationItem(syndicationItem, namedIndex);

            //Fire adding event
            IndexingService.OnDocumentAdding(this, new AddUpdateEventArgs(doc, namedIndex.Name));

            WriteToIndex(id, doc, namedIndex);

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End adding document with id field: '{0}' to index: '{1}'", id, namedIndex.Name));

            //Fire added event
            IndexingService.OnDocumentAdded(this, new AddUpdateEventArgs(doc, namedIndex.Name));
        }

        private void Update(SyndicationItem item, NamedIndex namedIndex)
        {
            // Store old virtual path values if they are needed later to update virtual paths for subnodes
            string oldVirtualPath = String.Empty;
            string newVirtualPath = String.Empty;

            if (GetAutoUpdateVirtualPathValue(item))
            {
                Document doc = GetDocumentById(item.Id, namedIndex);

                if (doc != null)
                {
                    oldVirtualPath = doc.Get(IndexingServiceSettings.VirtualPathFieldName);
                    newVirtualPath = new VirtualPathFieldStoreSerializer(item).ToFieldStoreValue();
                }
            }

            Remove(item.Id, namedIndex, false);
            Add(item, namedIndex);

            UpdateVirtualPaths(oldVirtualPath, newVirtualPath);
        }

        private void Remove(SyndicationItem item, NamedIndex namedIndex)
        {
            // We could have recieved remove requests that only should affect virtual paths
            if (!string.Equals(item.Id, IgnoreItemId))
            {
                Remove(item.Id, namedIndex, true);
            }

            // If AutoUpdate is set, delete all items with the provided virtual path or 
            if (GetAutoUpdateVirtualPathValue(item))
            {
                var virtualPath = new VirtualPathFieldStoreSerializer(item).ToFieldStoreValue();
                RemoveByVirtualPath(virtualPath);
            }
        }

        private bool GetAutoUpdateVirtualPathValue(SyndicationItem item)
        {
            bool autoUpdateVirtualPath;
            if (bool.TryParse(GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameAutoUpdateVirtualPath), out autoUpdateVirtualPath))
            {
                return autoUpdateVirtualPath;
            }
            return false;
        }

        /// <summary>
        /// Removes a document from the specified named index by the id-field
        /// </summary>
        /// <param name="id">The id field of the document to remove</param>
        /// <param name="namedIndexName">The named index from where to remove the document</param>
        private void Remove(string itemId, NamedIndex namedIndex, bool removeRef)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start deleting Lucene document with id field: '{0}' from index '{1}'", itemId, namedIndex.Name));

            IndexingService.OnDocumentRemoving(this, new RemoveEventArgs(itemId, namedIndex.Name));

            bool succeeded = DeleteFromIndex(namedIndex, itemId, removeRef);

            if (succeeded)
            {
                IndexingService.OnDocumentRemoved(this, new RemoveEventArgs(itemId, namedIndex.Name));

                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End deleting document with id field: '{0}'", itemId));
            }
        }

        private void RemoveByVirtualPath(string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath))
            {
                return;
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start removing all items under virtual path '{0}'.", virtualPath));

            //Get all documents under old virtual path for all named indexes
            Collection<NamedIndex> allNamedIndexes = new Collection<NamedIndex>();
            foreach (string name in IndexingServiceSettings.NamedIndexElements.Keys)
            {
                allNamedIndexes.Add(new NamedIndex(name));
            }

            int totalHits = 0;
            Collection<ScoreDocument> scoreDocuments =
                GetScoreDocuments(String.Format("{0}:{1}*", IndexingServiceSettings.VirtualPathFieldName, virtualPath),
                false, allNamedIndexes, 0, IndexingServiceSettings.MaxHitsForReferenceSearch,
                IndexingServiceSettings.MaxHitsForReferenceSearch, out totalHits);

            foreach (ScoreDocument scoreDocument in scoreDocuments)
            {
                Document doc = scoreDocument.Document;
                NamedIndex namedIndex = new NamedIndex(doc.Get(IndexingServiceSettings.NamedIndexFieldName));
                string id = doc.Get(IndexingServiceSettings.IdFieldName);

                Remove(id, namedIndex, true);
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End removing by virtual path."));
        }

        private void UpdateVirtualPaths(string oldVirtualPath, string newVirtualPath)
        {
            if (String.IsNullOrEmpty(newVirtualPath) || newVirtualPath.Equals(oldVirtualPath, StringComparison.InvariantCulture))
            {
                return;
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start updating virtual paths from old path: '{0}' to new path '{1}'", oldVirtualPath, newVirtualPath));

            //Get all documents under old virtual path for all named indexes
            Collection<NamedIndex> allNamedIndexes = new Collection<NamedIndex>();
            foreach (string name in IndexingServiceSettings.NamedIndexElements.Keys)
            {
                allNamedIndexes.Add(new NamedIndex(name));
            }

            int totalHits = 0;
            Collection<ScoreDocument> scoreDocuments =
                GetScoreDocuments(String.Format("{0}:{1}*", IndexingServiceSettings.VirtualPathFieldName, oldVirtualPath),
                false, allNamedIndexes, 0, IndexingServiceSettings.MaxHitsForReferenceSearch,
                IndexingServiceSettings.MaxHitsForReferenceSearch, out totalHits);

            foreach (ScoreDocument scoreDocument in scoreDocuments)
            {
                Document doc = scoreDocument.Document;
                NamedIndex namedIndex = new NamedIndex(doc.Get(IndexingServiceSettings.NamedIndexFieldName));

                string id = doc.Get(IndexingServiceSettings.IdFieldName);
                string vp = doc.Get(IndexingServiceSettings.VirtualPathFieldName);
                vp = vp.Remove(0, oldVirtualPath.Length);
                vp = vp.Insert(0, newVirtualPath);
                doc.RemoveField(IndexingServiceSettings.VirtualPathFieldName);
                doc.Add(new Field(IndexingServiceSettings.VirtualPathFieldName, vp,
                    IndexingServiceSettings.FieldProperties[IndexingServiceSettings.VirtualPathFieldName].FieldStore,
                    IndexingServiceSettings.FieldProperties[IndexingServiceSettings.VirtualPathFieldName].FieldIndex));

                AddAllSearchableContentsFieldToDocument(doc, namedIndex);

                // Remove and add the document
                Remove(id, namedIndex, false);
                WriteToIndex(id, doc, namedIndex);

                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Updated virtual path for document with id: '{0}'.", id));
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End updating virtual paths"));
        }

        private void AddAllSearchableContentsFieldToDocument(Document doc, NamedIndex namedIndex)
        {
            string id = doc.Get(IndexingServiceSettings.IdFieldName);

            StringBuilder totalContents = new StringBuilder();
            totalContents.Append(doc.Get(IndexingServiceSettings.TitleFieldName));
            totalContents.Append(" ");
            totalContents.Append(doc.Get(IndexingServiceSettings.DisplayTextFieldName));
            totalContents.Append(" ");
            totalContents.Append(doc.Get(IndexingServiceSettings.MetadataFieldName));
            totalContents.Append(" ");
            totalContents.Append(GetReferenceData(id, namedIndex));

            doc.RemoveField(IndexingServiceSettings.DefaultFieldName);
            doc.Add(new Field(IndexingServiceSettings.DefaultFieldName, totalContents.ToString(), 
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.DefaultFieldName].FieldStore, 
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.DefaultFieldName].FieldIndex));
        }

        private Document GetDocumentFromSyndicationItem(SyndicationItem syndicationItem, NamedIndex namedIndex)
        {
            string id = syndicationItem.Id;
            string authors = PrepareAuthors(syndicationItem);
            string title = (syndicationItem.Title != null) ? syndicationItem.Title.Text : "";
            string displayText = (syndicationItem.Content != null) ? ((TextSyndicationContent)syndicationItem.Content).Text : "";
            DateTime created = (syndicationItem.PublishDate.DateTime.Year < 2) ? DateTime.Now : syndicationItem.PublishDate.DateTime;
            DateTime modified = (syndicationItem.LastUpdatedTime.DateTime.Year < 2) ? DateTime.Now : syndicationItem.LastUpdatedTime.DateTime;
            string url = (syndicationItem.BaseUri != null) ? syndicationItem.BaseUri.ToString() : "";
            string boostFactor = GetAttributeValue(syndicationItem, IndexingServiceSettings.SyndicationItemAttributeNameBoostFactor);
            string culture = GetAttributeValue(syndicationItem, IndexingServiceSettings.SyndicationItemAttributeNameCulture);
            string type = GetAttributeValue(syndicationItem, IndexingServiceSettings.SyndicationItemAttributeNameType);
            string referenceId = GetAttributeValue(syndicationItem, IndexingServiceSettings.SyndicationItemAttributeNameReferenceId);
            string metadata = GetElementValue(syndicationItem, IndexingServiceSettings.SyndicationItemElementNameMetadata);
            string itemStatus = GetAttributeValue(syndicationItem, IndexingServiceSettings.SyndicationItemAttributeNameItemStatus);

            DateTime publicationEnd;
            bool hasExpiration = false;
            if (DateTime.TryParse(GetAttributeValue(syndicationItem, IndexingServiceSettings.SyndicationItemAttributeNamePublicationEnd), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out publicationEnd))
            {
                hasExpiration = true;
            }

            DateTime publicationStart;
            bool hasStart = false;
            if (DateTime.TryParse(GetAttributeValue(syndicationItem, IndexingServiceSettings.SyndicationItemAttributeNamePublicationStart), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out publicationStart))
            {
                hasStart = true;
            }

            CategoriesFieldStoreSerializer categoriesSerializer = new CategoriesFieldStoreSerializer(syndicationItem);
            AclFieldStoreSerializer aclSerializer = new AclFieldStoreSerializer(syndicationItem);
            VirtualPathFieldStoreSerializer virtualPathSerializer = new VirtualPathFieldStoreSerializer(syndicationItem);
            AuthorsFieldStoreSerializer authorsSerializer = new AuthorsFieldStoreSerializer(syndicationItem);

            // Split displayText
            string displayTextOut = String.Empty;
            string metadataOut = String.Empty;
            SplitDisplayTextToMetadata(displayText, metadata, out displayTextOut, out metadataOut);

            //Create the document
            Document doc = new Document();
            doc.Add(new Field(IndexingServiceSettings.IdFieldName, id, 
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.IdFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.IdFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.TitleFieldName, title,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.TitleFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.TitleFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.DisplayTextFieldName, displayTextOut,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.DisplayTextFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.DisplayTextFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.CreatedFieldName, Regex.Replace(created.ToString("u", CultureInfo.InvariantCulture), @"\D", ""),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.CreatedFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.CreatedFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.ModifiedFieldName, Regex.Replace(modified.ToString("u", CultureInfo.InvariantCulture), @"\D", ""),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.ModifiedFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.ModifiedFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.PublicationEndFieldName, hasExpiration ? Regex.Replace(publicationEnd.ToUniversalTime().ToString("u"), @"\D", "") : "no",
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.PublicationEndFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.PublicationEndFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.PublicationStartFieldName, hasStart ? Regex.Replace(publicationStart.ToUniversalTime().ToString("u"), @"\D", "") : "no",
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.PublicationStartFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.PublicationStartFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.UriFieldName, url,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.UriFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.UriFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.MetadataFieldName, metadataOut,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.MetadataFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.MetadataFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.CategoriesFieldName, categoriesSerializer.ToFieldStoreValue(),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.CategoriesFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.CategoriesFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.CultureFieldName, culture,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.CultureFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.CultureFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.AuthorsFieldName, authors,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.AuthorsFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.AuthorsFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.TypeFieldName, type,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.TypeFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.TypeFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.ReferenceIdFieldName, referenceId,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.ReferenceIdFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.ReferenceIdFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.AclFieldName, aclSerializer.ToFieldStoreValue(),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.AclFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.AclFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.VirtualPathFieldName, virtualPathSerializer.ToFieldStoreValue(),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.VirtualPathFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.VirtualPathFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.AuthorStorageFieldName, authorsSerializer.ToFieldStoreValue(),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.AuthorStorageFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.AuthorStorageFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.NamedIndexFieldName, namedIndex.Name,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.NamedIndexFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.NamedIndexFieldName].FieldIndex));

            doc.Add(new Field(IndexingServiceSettings.ItemStatusFieldName, itemStatus,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.ItemStatusFieldName].FieldStore,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.ItemStatusFieldName].FieldIndex));

            AddAllSearchableContentsFieldToDocument(doc, namedIndex);

            float fltBoostFactor = 1;
            doc.Boost = ((float.TryParse(boostFactor, out fltBoostFactor)) ? fltBoostFactor : 1);

            return doc;
        }

        private string GetReferenceData(string referenceId, NamedIndex namedIndex)
        {
            if (namedIndex.ReferenceDirectory == null)
            {
                // This is a parent item
                return String.Empty;
            }

            StringBuilder sb = new StringBuilder();

            try
            {
                namedIndex = new NamedIndex(namedIndex.Name, true);
                int totalHits = 0;
                Collection<ScoreDocument> scoreDocuments =
                    SingleIndexSearch(String.Format("{0}:{1}",
                    IndexingServiceSettings.ReferenceIdFieldName, QueryParser.Escape(referenceId)), namedIndex, IndexingServiceSettings.MaxHitsForReferenceSearch, out totalHits);

                foreach (ScoreDocument scoreDocument in scoreDocuments)
                {
                    Document hitDoc = scoreDocument.Document;
                    sb.Append(hitDoc.Get(IndexingServiceSettings.TitleFieldName));
                    sb.Append(" ");
                    sb.Append(hitDoc.Get(IndexingServiceSettings.DisplayTextFieldName));
                    sb.Append(" ");
                    sb.Append(hitDoc.Get(IndexingServiceSettings.MetadataFieldName));
                    sb.Append(" ");
                }
            }
            catch (Exception e)
            {
                IndexingServiceSettings.HandleServiceError(String.Format("Could not get reference data for id: {0}. Message: {1}{2}{3}", referenceId, e.Message, Environment.NewLine, e.StackTrace));
                return null;
            }

            return sb.ToString();
        }

        private SyndicationItem GetSyndicationItemFromDocument(ScoreDocument scoreDocument)
        {
            Document doc = scoreDocument.Document;

            // Create namedIndex for this document
            NamedIndex namedIndex = new NamedIndex(doc.Get(IndexingServiceSettings.NamedIndexFieldName));

            //Create search result object and add it to result collection
            SyndicationItem syndicationItem = new SyndicationItem();

            // ID field
            syndicationItem.Id = namedIndex.IncludeInResponse(IndexingServiceSettings.IdFieldName) ? doc.Get(IndexingServiceSettings.IdFieldName) : "";

            // Title field
            syndicationItem.Title = namedIndex.IncludeInResponse(IndexingServiceSettings.TitleFieldName) ? new TextSyndicationContent(doc.Get(IndexingServiceSettings.TitleFieldName)) : new TextSyndicationContent("");

            // DisplayText field
            syndicationItem.Content = namedIndex.IncludeInResponse(IndexingServiceSettings.DisplayTextFieldName) ? new TextSyndicationContent(doc.Get(IndexingServiceSettings.DisplayTextFieldName)) : new TextSyndicationContent("");

            // Modified field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.ModifiedFieldName))
            {
                syndicationItem.LastUpdatedTime = new DateTimeOffset(Convert.ToDateTime(Regex.Replace(doc.Get(IndexingServiceSettings.ModifiedFieldName), @"(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})", "$1-$2-$3 $4:$5:$6"), CultureInfo.InvariantCulture));
            }

            // Created field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.CreatedFieldName))
            {
                syndicationItem.PublishDate = new DateTimeOffset(Convert.ToDateTime(Regex.Replace(doc.Get(IndexingServiceSettings.CreatedFieldName), @"(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})", "$1-$2-$3 $4:$5:$6"), CultureInfo.InvariantCulture));
            }

            // Uri field
            Uri uri;
            if (Uri.TryCreate(doc.Get(IndexingServiceSettings.UriFieldName), UriKind.RelativeOrAbsolute, out uri))
            {
                syndicationItem.BaseUri = namedIndex.IncludeInResponse(IndexingServiceSettings.UriFieldName) ? uri : null;
            }

            // Culture field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.CultureFieldName))
            {
                syndicationItem.AttributeExtensions.Add(new XmlQualifiedName(IndexingServiceSettings.SyndicationItemAttributeNameCulture, IndexingServiceSettings.XmlQualifiedNamespace), doc.Get(IndexingServiceSettings.CultureFieldName));
            }

            // ItemStatus field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.ItemStatusFieldName))
            {
                syndicationItem.AttributeExtensions.Add(new XmlQualifiedName(IndexingServiceSettings.SyndicationItemAttributeNameItemStatus, IndexingServiceSettings.XmlQualifiedNamespace), doc.Get(IndexingServiceSettings.ItemStatusFieldName));
            }

            // Type field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.TypeFieldName))
            {
                syndicationItem.AttributeExtensions.Add(new XmlQualifiedName(IndexingServiceSettings.SyndicationItemAttributeNameType, IndexingServiceSettings.XmlQualifiedNamespace), doc.Get(IndexingServiceSettings.TypeFieldName));
            }

            // Score field not optional. Always included
            syndicationItem.AttributeExtensions.Add(new XmlQualifiedName(IndexingServiceSettings.SyndicationItemAttributeNameScore, IndexingServiceSettings.XmlQualifiedNamespace), scoreDocument.Score.ToString(CultureInfo.InvariantCulture));

            // Data Uri not optional. Always included
            syndicationItem.AttributeExtensions.Add(new XmlQualifiedName(IndexingServiceSettings.SyndicationItemAttributeNameDataUri, IndexingServiceSettings.XmlQualifiedNamespace), doc.Get(IndexingServiceSettings.SyndicationItemAttributeNameDataUri));

            // Boost factor not optional. Always included
            syndicationItem.AttributeExtensions.Add(new XmlQualifiedName(IndexingServiceSettings.SyndicationItemAttributeNameBoostFactor, IndexingServiceSettings.XmlQualifiedNamespace), doc.Boost.ToString(CultureInfo.InvariantCulture));

            // Named index not optional. Always included
            syndicationItem.AttributeExtensions.Add(new XmlQualifiedName(IndexingServiceSettings.SyndicationItemAttributeNameNamedIndex, IndexingServiceSettings.XmlQualifiedNamespace), doc.Get(IndexingServiceSettings.NamedIndexFieldName));

            // PublicationEnd field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.PublicationEndFieldName))
            {
                string publicationEnd = doc.Get(IndexingServiceSettings.PublicationEndFieldName);
                if (!publicationEnd.Equals("no"))
                {
                    syndicationItem.AttributeExtensions.Add(new XmlQualifiedName(IndexingServiceSettings.SyndicationItemAttributeNamePublicationEnd, IndexingServiceSettings.XmlQualifiedNamespace), Convert.ToDateTime(Regex.Replace(publicationEnd, @"(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})", "$1-$2-$3 $4:$5:$6Z"), CultureInfo.InvariantCulture).ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
                }
            }

            // PublicationStart field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.PublicationStartFieldName))
            {
                string publicationStart = doc.Get(IndexingServiceSettings.PublicationStartFieldName);
                if (!publicationStart.Equals("no"))
                {
                    syndicationItem.AttributeExtensions.Add(new XmlQualifiedName(IndexingServiceSettings.SyndicationItemAttributeNamePublicationStart, IndexingServiceSettings.XmlQualifiedNamespace), Convert.ToDateTime(Regex.Replace(publicationStart, @"(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})", "$1-$2-$3 $4:$5:$6Z"), CultureInfo.InvariantCulture).ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
                }
            }

            //Metadata field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.MetadataFieldName))
            {
                syndicationItem.ElementExtensions.Add(new SyndicationElementExtension(IndexingServiceSettings.SyndicationItemElementNameMetadata,
                    IndexingServiceSettings.XmlQualifiedNamespace, doc.Get(IndexingServiceSettings.MetadataFieldName)));
            }

            // Categories
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.CategoriesFieldName))
            {
                string fieldStoreValue = doc.Get(IndexingServiceSettings.CategoriesFieldName);
                new CategoriesFieldStoreSerializer
                    (fieldStoreValue).
                    AddFieldStoreValueToSyndicationItem(syndicationItem);
            }

            //Authors 
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.AuthorsFieldName))
            {
                string fieldStoreValue = doc.Get(IndexingServiceSettings.AuthorStorageFieldName);
                new AuthorsFieldStoreSerializer
                    (fieldStoreValue).AddFieldStoreValueToSyndicationItem(syndicationItem);
            }

            //RACL to syndication item
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.AclFieldName))
            {
                string fieldStoreValue = doc.Get(IndexingServiceSettings.AclFieldName);
                new AclFieldStoreSerializer
                    (fieldStoreValue).AddFieldStoreValueToSyndicationItem(syndicationItem);
            }

            //Virtual path to syndication item
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.VirtualPathFieldName))
            {
                string fieldStoreValue = doc.Get(IndexingServiceSettings.VirtualPathFieldName);
                new VirtualPathFieldStoreSerializer
                    (fieldStoreValue).
                    AddFieldStoreValueToSyndicationItem(syndicationItem);
            }

            return syndicationItem;
        }

        /// <summary>
        /// Gets whether the supplied named index exists
        /// </summary>
        /// <param name="namedIndexName">the name of the named index</param>
        /// <returns></returns>
        private static bool IsValidIndex(string namedIndexName)
        {
            if (String.IsNullOrEmpty(namedIndexName))
            {
                namedIndexName = IndexingServiceSettings.DefaultIndexName;
                if (IndexingServiceSettings.NamedIndexDirectories.ContainsKey(namedIndexName))
                {
                    return true;
                }
            }
            else if (IndexingServiceSettings.NamedIndexDirectories.ContainsKey(namedIndexName))
            {
                return true;
            }
            IndexingServiceSettings.HandleServiceError(String.Format("Named index \"{0}\" is not valid, it does not exist in configuration or has faulty configuration", namedIndexName));
            return false;
        }

        private Document GetDocumentById(string id, NamedIndex namedIndex)
        {
            int totalHits = 0;
            Collection<ScoreDocument> docs = SingleIndexSearch(String.Format("{0}:{1}",
                IndexingServiceSettings.IdFieldName,
                QueryParser.Escape(id)), namedIndex, 1, out totalHits);

            if (docs.Count > 0)
                return docs[0].Document;
            else
                return null;
        }

        private bool DeleteFromIndex(NamedIndex namedIndex, string itemId, bool deleteRef)
        {
            ReaderWriterLockSlim rwl = IndexingServiceSettings.ReaderWriterLocks[namedIndex.Name];

            Term term = null;

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start deleting Lucene document with id field '{0}' from index '{1}'", itemId, namedIndex.Name));

            int i = 0;
            int pendingDeletions = 0;

            rwl.EnterWriteLock();
            try
            {
                using (var reader = IndexReader.Open(namedIndex.Directory, false))
                {
                    term = new Term(IndexingServiceSettings.IdFieldName, itemId);
                    i = reader.DeleteDocuments(term);

                    pendingDeletions = reader.NumDeletedDocs;
                }
            }
            catch (Exception e)
            {
                IndexingServiceSettings.HandleServiceError(String.Format("Failed to delete Document with id: {0}. Message: {1}{2}{3}", itemId.ToString(), e.Message, Environment.NewLine, e.StackTrace)); 
                return false;
            }
            finally
            {
                rwl.ExitWriteLock();
            }

            if (i == 0) // Document didn't exist
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Failed to delete Document with id: {0}. Document does not exist.", itemId.ToString()));
                return false;
            }
            else
            {
                // Delete any referencing documents
                if (deleteRef && namedIndex.ReferenceDirectory != null)
                {
                    IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start deleting reference documents for id '{0}'", itemId.ToString()));

                    ReaderWriterLockSlim rwlRef = IndexingServiceSettings.ReaderWriterLocks[namedIndex.ReferenceName];
                    rwlRef.EnterWriteLock();

                    try
                    {
                        using (var refReader = IndexReader.Open(namedIndex.ReferenceDirectory, false))
                        {
                            Term refTerm = new Term(IndexingServiceSettings.ReferenceIdFieldName, itemId);
                            refReader.DeleteDocuments(refTerm);
                        }
                    }
                    catch (Exception e)
                    {
                        IndexingServiceSettings.HandleServiceError(String.Format("Failed to delete referencing Documents for reference id: {0}. Message: {1}{2}{3}", itemId.ToString(), e.Message, Environment.NewLine, e.StackTrace));
                        return false;
                    }
                    finally
                    {                        
                        rwlRef.ExitWriteLock();
                    }

                    IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End deleting reference documents for id '{0}'", itemId.ToString()));
                }

                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End deleting Lucene document with id field: '{0}'", itemId));

                // Optimize index
                if ((namedIndex.PendingDeletesOptimizeThreshold > 0) && 
                    (pendingDeletions >= namedIndex.PendingDeletesOptimizeThreshold))
                {
                    OptimizeIndex(namedIndex);
                }

                return true;
            }
        }

        /// <summary>
        /// return true or false depending on if the <see cref="Document"/> exists in the supplied named index <see cref="NamedIndex.Name"/>
        /// </summary>
        /// <param name="id">The <see cref="SearchableItem"/> ID to check for existance</param>
        /// <param name="namedIndex">The <see cref="NamedIndex"/> telling which index to search</param>
        /// <returns></returns>
        private bool DocumentExists(string itemId, NamedIndex namedIndex)
        {
            try
            {
                if (GetDocumentById(itemId, namedIndex) != null)
                    return true;
            }
            catch (Exception e)
            {
                IndexingServiceSettings.HandleServiceError(String.Format("Could not verify document existense for id: '{0}'. Message: {1}{2}{3}", itemId, e.Message, Environment.NewLine, e.StackTrace));
            }
            return false;
        }

        private void WriteToIndex(string itemId, Document doc, NamedIndex namedIndex)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start writing document with id '{0}' to index '{1}' with analyzer '{2}'", itemId, namedIndex.Name, IndexingServiceSettings.Analyzer.ToString()));

            // Write to Directory
            if (DocumentExists(itemId, namedIndex))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Failed to write to index: '{0}'. Document with id: '{1}' already exists", namedIndex.Name, itemId));
                return;
            }

            ReaderWriterLockSlim rwl = IndexingServiceSettings.ReaderWriterLocks[namedIndex.Name];

            rwl.EnterWriteLock();

            try
            {
                using (var iWriter = new IndexWriter(namedIndex.Directory, IndexingServiceSettings.Analyzer, false, Lucene.Net.Index.IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    iWriter.AddDocument(doc);
                }
            }
            catch (Exception e)
            {
                IndexingServiceSettings.HandleServiceError(String.Format("Failed to write to index: '{0}'. Message: {1}{2}{3}", namedIndex.Name, e.Message, Environment.NewLine, e.StackTrace));    
                return;
            }
            finally
            {
                rwl.ExitWriteLock();
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End writing to index"));
        }

        private void OptimizeIndex(NamedIndex namedIndex)
        {
            ReaderWriterLockSlim rwl = IndexingServiceSettings.ReaderWriterLocks[namedIndex.Name];

            rwl.EnterWriteLock();

            try
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start optimizing index"));

                using (var iWriter = new IndexWriter(namedIndex.Directory, IndexingServiceSettings.Analyzer, false, Lucene.Net.Index.IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    iWriter.Optimize();
                }

                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End optimizing index"));

            }
            catch (Exception e)
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Error(String.Format("Failed to optimize index: '{0}'. Message: {1}{2}{3}", namedIndex.Name, e.Message, Environment.NewLine, e.StackTrace));
            }
            finally
            {
                rwl.ExitWriteLock();
            }

            //Fire event
            IndexingService.OnIndexedOptimized(this, new OptimizedEventArgs(namedIndex.Name));

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Optimized index: '{0}'", namedIndex.Name));
        }

        private Collection<ScoreDocument> SingleIndexSearch(string q, NamedIndex namedIndex, int maxHits, out int totalHits)
        {
            Collection<ScoreDocument> scoreDocuments = new Collection<ScoreDocument>();
            totalHits = 0;
            ReaderWriterLockSlim rwl = IndexingServiceSettings.ReaderWriterLocks[namedIndex.Name];
            rwl.EnterReadLock();

            try
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Creating Lucene QueryParser for index '{0}' with expression '{1}' with analyzer '{2}'", namedIndex.Name, q, IndexingServiceSettings.Analyzer.ToString()));
                QueryParser parser = new PerFieldQueryParserWrapper(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.DefaultFieldName, IndexingServiceSettings.Analyzer, IndexingServiceSettings.LowercaseFields);
                Query baseQuery = parser.Parse(q);
                using (var searcher = new IndexSearcher(namedIndex.Directory, true))
                {
                    TopDocs topDocs = searcher.Search(baseQuery, maxHits);
                    totalHits = topDocs.TotalHits;
                    ScoreDoc[] docs = topDocs.ScoreDocs;
                    for (int i = 0; i < docs.Length; i++)
                    {
                        scoreDocuments.Add(new ScoreDocument(searcher.Doc(docs[i].Doc), docs[i].Score));
                    }
                }
            }
            catch (Exception e)
            {
                IndexingServiceSettings.HandleServiceError(String.Format("Failed to search index '{0}'. Index seems to be corrupt! Message: {1}{2}{3}", namedIndex.Name, e.Message, Environment.NewLine, e.StackTrace));
            }
            finally
            {
                rwl.ExitReadLock();
            }

            return scoreDocuments;
        }

        private static Collection<ScoreDocument> MultiIndexSearch(string q, Collection<NamedIndex> namedIndexes, int maxHits, out int totalHits)
        {
            //Prepare queries for MultiSearcher
            Query[] queries = new Query[namedIndexes.Count];
            IndexSearcher[] searchers = new IndexSearcher[namedIndexes.Count];
            Collection<ReaderWriterLockSlim> locks = new Collection<ReaderWriterLockSlim>();

            //Modify queries for other indexes with other field names
            int i = 0;
            foreach (NamedIndex namedIndex in namedIndexes)
            {
                string defaultFieldName = IndexingServiceSettings.DefaultFieldName;

                ReaderWriterLockSlim rwl = IndexingServiceSettings.ReaderWriterLocks[namedIndex.Name];
                locks.Add(rwl);
                rwl.EnterReadLock();

                try
                {
                    IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Creating Lucene QueryParser for index '{0}' with expression '{1}' with analyzer '{2}'", namedIndex.Name, q, IndexingServiceSettings.Analyzer.ToString()));
                    QueryParser parser = new PerFieldQueryParserWrapper(IndexingServiceSettings.LuceneVersion, defaultFieldName, IndexingServiceSettings.Analyzer, IndexingServiceSettings.LowercaseFields);
                    queries[i] = parser.Parse(q);
                    searchers[i] = new IndexSearcher(namedIndex.Directory, true);
                }
                catch (Exception e)
                {
                    IndexingServiceSettings.HandleServiceError(String.Format("Failed to create sub searcher for index '{0}' Message: {1}{2}{3}", namedIndex.Name, e.Message, Environment.NewLine, e.StackTrace));
                }
                finally
                {
                    rwl.ExitReadLock();
                }

                i++;
            }

            Query combinedQuery = queries[0].Combine(queries);
            Collection<ScoreDocument> scoreDocuments = new Collection<ScoreDocument>();
            totalHits = 0;

            // Read locks
            foreach (ReaderWriterLockSlim rwl in locks)
            {
                rwl.EnterReadLock();
            }

            try
            {
                using (MultiSearcher multiSearcher = new MultiSearcher(searchers))
                {
                    TopDocs topDocs = multiSearcher.Search(combinedQuery, maxHits);
                    totalHits = topDocs.TotalHits;
                    ScoreDoc[] docs = topDocs.ScoreDocs;
                    for (int j = 0; j < docs.Length; j++)
                    {
                        scoreDocuments.Add(new ScoreDocument(multiSearcher.Doc(docs[j].Doc), docs[j].Score));
                    }
                }
            }
            catch (Exception e)
            {
                IndexingServiceSettings.HandleServiceError(String.Format("Failed to get hits from MultiSearcher! Message: {0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace));
            }
            finally
            {
                foreach (ReaderWriterLockSlim rwl in locks)
                {
                    rwl.ExitReadLock();
                }
            }

            return scoreDocuments;
        }

        private static bool IsModifyIndex(string namedIndexName)
        {
            if (String.IsNullOrEmpty(namedIndexName))
            {
                namedIndexName = IndexingServiceSettings.DefaultIndexName;
            }

            if (IndexingServiceSettings.NamedIndexElements[namedIndexName].ReadOnly)
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Error(String.Format("cannot modify index: '{0}'. Index is readonly.", namedIndexName));

                IndexingServiceSettings.SetResponseHeaderStatusCode(401);

                return false;
            }
            else
            {
                return true;
            }
        }

        private static string PrepareExpression(string q, bool excludeNotPublished)
        {
            string expression = q;
            expression = PrepareEscapeFields(expression, IndexingServiceSettings.CategoriesFieldName);
            expression = PrepareEscapeFields(expression, IndexingServiceSettings.AclFieldName);
            string currentDate = Regex.Replace(DateTime.Now.ToUniversalTime().ToString("u"), @"\D", "");

            if (excludeNotPublished)
            {
                expression = String.Format("({0}) AND ({1}:(no) OR {1}:[{2} TO 99999999999999])", expression, 
                    IndexingServiceSettings.PublicationEndFieldName, currentDate);
            }
            if (excludeNotPublished)
            {
                expression = String.Format("({0}) AND ({1}:(no) OR {1}:[00000000000000 TO {2}])", expression, 
                    IndexingServiceSettings.PublicationStartFieldName, currentDate);
            }

            return expression;
        }

        private static string PrepareEscapeFields(string q, string fieldName)
        {
            MatchEvaluator regexEscapeFields = delegate(Match m)
            {
                if (m.Groups["fieldname"].Value.Equals(fieldName + ":"))
                {
                    return m.Groups["fieldname"] + "\"" + IndexingServiceSettings.TagsPrefix + m.Groups["terms"].Value.Replace("(", "").Replace(")", "") + IndexingServiceSettings.TagsSuffix + "\"";
                }
                else
                {
                    return m.Groups[0].Value;
                }
            };

            string expr = Regex.Replace(q, "(?<fieldname>\\w+:)?(?:(?<terms>\\([^()]*\\))|(?<terms>[^\\s()\"]+)|(?<terms>\"[^\"]*\"))", regexEscapeFields);

            return expr;
        }

        private static string PrepareAuthors(SyndicationItem syndicationItem)
        {
            StringBuilder authors = new StringBuilder();
            if (syndicationItem.Authors != null)
            {
                foreach (SyndicationPerson person in syndicationItem.Authors)
                {
                    authors.Append(person.Name);
                    authors.Append(" ");
                }
            }
            return authors.ToString().Trim();
        }

        private static void SetElementValue(SyndicationItem item, string elementExtensionName, string value)
        {
            //Remove and add the element extension meta data
            SyndicationElementExtension newExt = new SyndicationElementExtension(elementExtensionName,
                                                              IndexingServiceSettings.XmlQualifiedNamespace,
                                                              value);

            int i = 0;
            foreach (SyndicationElementExtension ext in item.ElementExtensions)
            {
                if (ext.OuterName == elementExtensionName &&
                    ext.OuterNamespace == IndexingServiceSettings.XmlQualifiedNamespace)
                {
                    break;
                }
                i++;
            }

            item.ElementExtensions.RemoveAt(i);
            item.ElementExtensions.Add(newExt);
        }

        private static void SetAttributeValue(SyndicationItem item, string attributeExtensionName, string value)
        {
            item.AttributeExtensions[new XmlQualifiedName(attributeExtensionName, IndexingServiceSettings.XmlQualifiedNamespace)] = value;
        }

        private static string GetAttributeValue(SyndicationItem syndicationItem, string attributeName)
        {
            string value = String.Empty;
            if (syndicationItem.AttributeExtensions.ContainsKey(new XmlQualifiedName(attributeName, IndexingServiceSettings.XmlQualifiedNamespace)))
            {
                value = syndicationItem.AttributeExtensions[new XmlQualifiedName(attributeName, IndexingServiceSettings.XmlQualifiedNamespace)];
            }
            return value;
        }

        private static string GetElementValue(SyndicationItem syndicationItem, string elementName)
        {
            Collection<string> elements = syndicationItem.ElementExtensions.ReadElementExtensions<string>(elementName, IndexingServiceSettings.XmlQualifiedNamespace);
            string value = "";
            if (elements.Count > 0)
            {
                value = syndicationItem.ElementExtensions.ReadElementExtensions<string>(elementName, IndexingServiceSettings.XmlQualifiedNamespace).ElementAt<string>(0);
            }

            return value;
        }

        private void UpdateReference(string referenceId, string itemId, NamedIndex mainNamedIndex)
        {
            Document mainDoc = GetDocumentById(referenceId, mainNamedIndex);

            if (mainDoc == null)
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Error(String.Format("Could not find main document with id: '{0}' for referencing item id '{1}'. Continuing anyway, index will heal when main document is added/updated.", referenceId, itemId));        
                return;
            }

            AddAllSearchableContentsFieldToDocument(mainDoc, mainNamedIndex);

            //remove old parent document without removing its reference data
            Remove(referenceId, mainNamedIndex, false);
            // Add the man document again
            WriteToIndex(referenceId, mainDoc, mainNamedIndex);
        }

        private static void SplitDisplayTextToMetadata(string displayText, string metadata, out string displayTextOut, out string metadataOut)
        {
            displayTextOut = String.Empty;
            metadataOut = String.Empty;

            if (displayText.Length <= IndexingServiceSettings.MaxDisplayTextLength)
            {
                displayTextOut = displayText;
                metadataOut = metadata;
                return;
            }
            else
            {
                displayTextOut = displayText.Substring(0, IndexingServiceSettings.MaxDisplayTextLength);
                StringBuilder sb = new StringBuilder();
                sb.Append(metadata); // Add original data
                sb.Append(" ");
                sb.Append(displayText.Substring(IndexingServiceSettings.MaxDisplayTextLength, displayText.Length - IndexingServiceSettings.MaxDisplayTextLength));
                metadataOut = sb.ToString();
            }
        }

        private class DataUriQueueItem
        {
            private SyndicationItem _item;
            private NamedIndex _namedIndex;
            internal DataUriQueueItem(SyndicationItem item, NamedIndex namedIndex)
            {
                this._item = item;
                this._namedIndex = namedIndex;
            }

            internal void Do()
            {
                Instance.HandleDataUri(_item, _namedIndex);
            }
        }

        #region Get text from file

        private const IFILTER_INIT FILTERSETTINGS = IFILTER_INIT.IFILTER_INIT_INDEXING_ONLY |
                        IFILTER_INIT.IFILTER_INIT_APPLY_INDEX_ATTRIBUTES |
                        IFILTER_INIT.IFILTER_INIT_APPLY_CRAWL_ATTRIBUTES |
                        IFILTER_INIT.IFILTER_INIT_CANON_SPACES;

        private const Int32 BufferSize = 65536;

        private static string GetFileText(string path)
        {
            StringBuilder text = new StringBuilder();
            IFilter iflt = null;
            object iunk = null;
            int i = TextFilter.LoadIFilter(path, iunk, ref iflt);
            if (i != (int)IFilterReturnCodes.S_OK)
            {
                return null; //Cannot find a filter for file
            }

            IFilterReturnCodes scode;
            //ArrayList textItems = new ArrayList();

            int attr = 0;
            IFILTER_FLAGS flagsSet = 0;
            scode = iflt.Init(FILTERSETTINGS, attr, IntPtr.Zero, ref flagsSet);
            if (scode != IFilterReturnCodes.S_OK)
            {
                throw new Exception(
                    String.Format("IFilter initialisation failed: {0}",
                    Enum.GetName(scode.GetType(), scode)));
            }

            while (scode == IFilterReturnCodes.S_OK)
            {
                STAT_CHUNK stat = new STAT_CHUNK();

                scode = iflt.GetChunk(ref stat);
                if (scode == IFilterReturnCodes.S_OK)
                {
                    if (stat.flags == CHUNKSTATE.CHUNK_TEXT)
                    {
                        if (text.Length > 0 && stat.breakType != CHUNK_BREAKTYPE.CHUNK_NO_BREAK)
                        {
                            text.AppendLine();
                        }
                        int bufSize = BufferSize;

                        IFilterReturnCodes scodeText = IFilterReturnCodes.S_OK;
                        StringBuilder tmpbuf = new StringBuilder(bufSize);

                        while (scodeText == IFilterReturnCodes.S_OK)
                        {
                            scodeText = iflt.GetText(ref bufSize, tmpbuf);
                            if (scodeText == IFilterReturnCodes.S_OK || scodeText == IFilterReturnCodes.FILTER_S_LAST_TEXT)
                            {
                                if (bufSize > 0)
                                {
                                    text.Append(tmpbuf.ToString(0, (bufSize > tmpbuf.Length ? tmpbuf.Length : bufSize)));
                                }
                            }

                            // We don't need to call again to get FILTER_E_END_OF_CHUNKS
                            if (scodeText == IFilterReturnCodes.FILTER_S_LAST_TEXT)
                            {
                                break;
                            }

                            bufSize = BufferSize;
                        }
                    }
                }
            }

            Marshal.ReleaseComObject(iflt);

            return text.ToString();
        }

        #endregion

        #endregion

    }
}

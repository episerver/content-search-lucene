using EPiServer.Search.IndexingService.Controllers;
using EPiServer.Search.IndexingService.FieldSerializers;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService.Helpers
{
    public class LuceneHelper : ILuceneHelper
    {
        /// <summary>
        /// Id that indicates that the item itself should be ignored - but side effects should be processed
        /// </summary>
        private const string IgnoreItemId = "<IgnoreItemId>";
        private readonly IFeedHelper _feedHelper;
        private readonly IResponseExceptionHelper _responseExceptionHelper;
        private readonly ICommonFunc _commonFunc;
        public LuceneHelper(IFeedHelper feedHelper,
            IResponseExceptionHelper responseExceptionHelper,
            ICommonFunc commonFunc)
        {
            _feedHelper = feedHelper;
            _responseExceptionHelper = responseExceptionHelper;
            _commonFunc = commonFunc;
        }

        public Document GetDocumentFromSyndicationItem(FeedItemModel feedItem, NamedIndex namedIndex)
        {
            string id = feedItem.Id;
            string authors = _feedHelper.PrepareAuthors(feedItem);
            string title = feedItem.Title;
            string displayText = feedItem.DisplayText;
            DateTime created = (feedItem.Created.Year < 2) ? DateTime.Now : feedItem.Created.DateTime;
            DateTime modified = (feedItem.Modified.Year < 2) ? DateTime.Now : feedItem.Modified.DateTime;
            string url = (feedItem.Uri != null) ? feedItem.Uri.ToString() : "";
            string boostFactor = _feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNameBoostFactor);
            string culture = _feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNameCulture);
            string type = _feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNameType);
            string referenceId = _feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNameReferenceId);
            string metadata = _feedHelper.GetElementValue(feedItem, IndexingServiceSettings.SyndicationItemElementNameMetadata);
            string itemStatus = _feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNameItemStatus);

            DateTime publicationEnd;
            bool hasExpiration = false;
            if (DateTime.TryParse(_feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNamePublicationEnd), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out publicationEnd))
            {
                hasExpiration = true;
            }

            DateTime publicationStart;
            bool hasStart = false;
            if (DateTime.TryParse(_feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNamePublicationStart), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out publicationStart))
            {
                hasStart = true;
            }

            CategoriesFieldStoreSerializer categoriesSerializer = new CategoriesFieldStoreSerializer(feedItem);
            AclFieldStoreSerializer aclSerializer = new AclFieldStoreSerializer(feedItem);
            VirtualPathFieldStoreSerializer virtualPathSerializer = new VirtualPathFieldStoreSerializer(feedItem);
            AuthorsFieldStoreSerializer authorsSerializer = new AuthorsFieldStoreSerializer(feedItem);

            // Split displayText
            string displayTextOut = String.Empty;
            string metadataOut = String.Empty;
            _commonFunc.SplitDisplayTextToMetadata(displayText, metadata, out displayTextOut, out metadataOut);

            //Create the document
            Document doc = new Document();
            doc.Add(new StringField(IndexingServiceSettings.IdFieldName, id,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.IdFieldName].FieldStore));

            doc.Add(new TextField(IndexingServiceSettings.TitleFieldName, title,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.TitleFieldName].FieldStore));

            doc.Add(new TextField(IndexingServiceSettings.DisplayTextFieldName, displayTextOut,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.DisplayTextFieldName].FieldStore));

            doc.Add(new StringField(IndexingServiceSettings.CreatedFieldName, Regex.Replace(created.ToString("u", CultureInfo.InvariantCulture), @"\D", ""),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.CreatedFieldName].FieldStore));

            doc.Add(new StringField(IndexingServiceSettings.ModifiedFieldName, Regex.Replace(modified.ToString("u", CultureInfo.InvariantCulture), @"\D", ""),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.ModifiedFieldName].FieldStore));

            doc.Add(new StringField(IndexingServiceSettings.PublicationEndFieldName, hasExpiration ? Regex.Replace(publicationEnd.ToUniversalTime().ToString("u"), @"\D", "") : "no",
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.PublicationEndFieldName].FieldStore));

            doc.Add(new StringField(IndexingServiceSettings.PublicationStartFieldName, hasStart ? Regex.Replace(publicationStart.ToUniversalTime().ToString("u"), @"\D", "") : "no",
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.PublicationStartFieldName].FieldStore));

            doc.Add(new StringField(IndexingServiceSettings.UriFieldName, url,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.UriFieldName].FieldStore));

            doc.Add(new StringField(IndexingServiceSettings.MetadataFieldName, metadataOut,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.MetadataFieldName].FieldStore));

            doc.Add(new TextField(IndexingServiceSettings.CategoriesFieldName, categoriesSerializer.ToFieldStoreValue(),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.CategoriesFieldName].FieldStore));

            doc.Add(new StringField(IndexingServiceSettings.CultureFieldName, culture,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.CultureFieldName].FieldStore));

            doc.Add(new TextField(IndexingServiceSettings.AuthorsFieldName, authors,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.AuthorsFieldName].FieldStore));

            doc.Add(new TextField(IndexingServiceSettings.TypeFieldName, type,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.TypeFieldName].FieldStore));

            doc.Add(new StringField(IndexingServiceSettings.ReferenceIdFieldName, referenceId,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.ReferenceIdFieldName].FieldStore));

            doc.Add(new TextField(IndexingServiceSettings.AclFieldName, aclSerializer.ToFieldStoreValue(),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.AclFieldName].FieldStore));

            doc.Add(new TextField(IndexingServiceSettings.VirtualPathFieldName, virtualPathSerializer.ToFieldStoreValue(),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.VirtualPathFieldName].FieldStore));

            doc.Add(new StringField(IndexingServiceSettings.AuthorStorageFieldName, authorsSerializer.ToFieldStoreValue(),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.AuthorStorageFieldName].FieldStore));

            doc.Add(new StringField(IndexingServiceSettings.NamedIndexFieldName, namedIndex.Name,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.NamedIndexFieldName].FieldStore));

            doc.Add(new StringField(IndexingServiceSettings.ItemStatusFieldName, itemStatus,
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.ItemStatusFieldName].FieldStore));

            AddAllSearchableContentsFieldToDocument(doc, namedIndex);

            float fltBoostFactor = 1;
            ((TextField)doc.Fields[1]).Boost = ((float.TryParse(boostFactor, out fltBoostFactor)) ? fltBoostFactor : 1);

            return doc;
        }

        public void AddAllSearchableContentsFieldToDocument(Document doc, NamedIndex namedIndex)
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
            doc.Add(new TextField(IndexingServiceSettings.DefaultFieldName, totalContents.ToString(),
                IndexingServiceSettings.FieldProperties[IndexingServiceSettings.DefaultFieldName].FieldStore));
        }

        public string GetReferenceData(string referenceId, NamedIndex namedIndex)
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
                _responseExceptionHelper.HandleServiceError(String.Format("Could not get reference data for id: {0}. Message: {1}{2}{3}", referenceId, e.Message, Environment.NewLine, e.StackTrace));
                return null;
            }

            return sb.ToString();
        }

        public void UpdateReference(string referenceId, string itemId, NamedIndex mainNamedIndex)
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

        public Document GetDocumentById(string id, NamedIndex namedIndex)
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

        public void WriteToIndex(string itemId, Document doc, NamedIndex namedIndex)
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
                IndexWriterConfig iwc = new IndexWriterConfig(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.Analyzer);
                iwc.OpenMode = OpenMode.CREATE_OR_APPEND;
                using (var iWriter = new IndexWriter(namedIndex.Directory, iwc))
                {
                    iWriter.AddDocument(doc);
                }
            }
            catch (Exception e)
            {
                _responseExceptionHelper.HandleServiceError(String.Format("Failed to write to index: '{0}'. Message: {1}{2}{3}", namedIndex.Name, e.Message, Environment.NewLine, e.StackTrace));
                return;
            }
            finally
            {
                rwl.ExitWriteLock();
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End writing to index"));
        }

        public void Add(FeedItemModel item, NamedIndex namedIndex)
        {
            if (item == null)
                return;

            string id = item.Id;

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start adding Lucene document with id field: '{0}' to index: '{1}'", id, namedIndex.Name));

            if (String.IsNullOrEmpty(id))
            {
                _responseExceptionHelper.HandleServiceError(String.Format("Failed to add Document. id field is missing."));
                return;
            }

            //Check if the document already exists.
            if (DocumentExists(id, namedIndex))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Document already exists. Skipping."));
                return;
            }

            Document doc = GetDocumentFromSyndicationItem(item, namedIndex);

            //Fire adding event
            IndexingController.OnDocumentAdding(this, new AddUpdateEventArgs(doc, namedIndex.Name));

            WriteToIndex(id, doc, namedIndex);

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End adding document with id field: '{0}' to index: '{1}'", id, namedIndex.Name));

            //Fire added event
            IndexingController.OnDocumentAdded(this, new AddUpdateEventArgs(doc, namedIndex.Name));
        }

        /// <summary>
        /// return true or false depending on if the <see cref="Document"/> exists in the supplied named index <see cref="NamedIndex.Name"/>
        /// </summary>
        /// <param name="id">The <see cref="SearchableItem"/> ID to check for existance</param>
        /// <param name="namedIndex">The <see cref="NamedIndex"/> telling which index to search</param>
        /// <returns></returns>
        public bool DocumentExists(string itemId, NamedIndex namedIndex)
        {
            try
            {
                if (GetDocumentById(itemId, namedIndex) != null)
                    return true;
            }
            catch (Exception e)
            {
                _responseExceptionHelper.HandleServiceError(String.Format("Could not verify document existense for id: '{0}'. Message: {1}{2}{3}", itemId, e.Message, Environment.NewLine, e.StackTrace));
            }
            return false;
        }

        public Collection<ScoreDocument> SingleIndexSearch(string q, NamedIndex namedIndex, int maxHits, out int totalHits)
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
                using (IndexReader reader = DirectoryReader.Open(namedIndex.Directory))
                {
                    var searcher = new IndexSearcher(reader);

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
                _responseExceptionHelper.HandleServiceError(String.Format("Failed to search index '{0}'. Index seems to be corrupt! Message: {1}{2}{3}", namedIndex.Name, e.Message, Environment.NewLine, e.StackTrace));
            }
            finally
            {
                rwl.ExitReadLock();
            }

            return scoreDocuments;
        }

        public void Remove(FeedItemModel feedItem, NamedIndex namedIndex)
        {
            // We could have recieved remove requests that only should affect virtual paths
            if (!string.Equals(feedItem.Id, IgnoreItemId))
            {
                Remove(feedItem.Id, namedIndex, true);
            }

            // If AutoUpdate is set, delete all items with the provided virtual path or 
            if (_feedHelper.GetAutoUpdateVirtualPathValue(feedItem))
            {
                var virtualPath = new VirtualPathFieldStoreSerializer(feedItem).ToFieldStoreValue();
                RemoveByVirtualPath(virtualPath);
            }
        }

        /// <summary>
        /// Removes a document from the specified named index by the id-field
        /// </summary>
        /// <param name="id">The id field of the document to remove</param>
        /// <param name="namedIndexName">The named index from where to remove the document</param>
        public void Remove(string itemId, NamedIndex namedIndex, bool removeRef)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start deleting Lucene document with id field: '{0}' from index '{1}'", itemId, namedIndex.Name));

            IndexingController.OnDocumentRemoving(this, new RemoveEventArgs(itemId, namedIndex.Name));

            bool succeeded = DeleteFromIndex(namedIndex, itemId, removeRef);

            if (succeeded)
            {
                IndexingController.OnDocumentRemoved(this, new RemoveEventArgs(itemId, namedIndex.Name));

                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End deleting document with id field: '{0}'", itemId));
            }
        }

        public void RemoveByVirtualPath(string virtualPath)
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

        public Collection<ScoreDocument> MultiIndexSearch(string q, Collection<NamedIndex> namedIndexes, int maxHits, out int totalHits)
        {
            //Prepare queries for MultiSearcher
            string defaultFieldName = IndexingServiceSettings.DefaultFieldName;
            IndexReader[] readers = new IndexReader[namedIndexes.Count];
            Collection<ReaderWriterLockSlim> locks = new Collection<ReaderWriterLockSlim>();

            //Modify queries for other indexes with other field names
            int i = 0;
            foreach (NamedIndex namedIndex in namedIndexes)
            {
                ReaderWriterLockSlim rwl = IndexingServiceSettings.ReaderWriterLocks[namedIndex.Name];
                locks.Add(rwl);
                rwl.EnterReadLock();

                try
                {
                    IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Creating Lucene QueryParser for index '{0}' with expression '{1}' with analyzer '{2}'", namedIndex.Name, q, IndexingServiceSettings.Analyzer.ToString()));
                    readers[i] = DirectoryReader.Open(namedIndex.Directory);
                }
                catch (Exception e)
                {
                    _responseExceptionHelper.HandleServiceError(String.Format("Failed to create sub searcher for index '{0}' Message: {1}{2}{3}", namedIndex.Name, e.Message, Environment.NewLine, e.StackTrace));
                }
                finally
                {
                    rwl.ExitReadLock();
                }

                i++;
            }

            QueryParser parser = new PerFieldQueryParserWrapper(IndexingServiceSettings.LuceneVersion, defaultFieldName, IndexingServiceSettings.Analyzer, IndexingServiceSettings.LowercaseFields);
            Query query = parser.Parse(q);
            Collection<ScoreDocument> scoreDocuments = new Collection<ScoreDocument>();
            totalHits = 0;

            // Read locks
            foreach (ReaderWriterLockSlim rwl in locks)
            {
                rwl.EnterReadLock();
            }

            try
            {
                using (MultiReader multiReader = new MultiReader(readers))
                {
                    IndexSearcher searcher = new IndexSearcher(multiReader);
                    TopDocs topDocs = searcher.Search(query, maxHits);
                    totalHits = topDocs.TotalHits;
                    ScoreDoc[] docs = topDocs.ScoreDocs;
                    for (int j = 0; j < docs.Length; j++)
                    {
                        scoreDocuments.Add(new ScoreDocument(searcher.Doc(docs[j].Doc), docs[j].Score));
                    }
                }
            }
            catch (Exception e)
            {
                _responseExceptionHelper.HandleServiceError(String.Format("Failed to get hits from MultiSearcher! Message: {0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace));
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

        public Collection<ScoreDocument> GetScoreDocuments(string q, bool excludeNotPublished, Collection<NamedIndex> namedIndexes, int offset, int limit, int maxHits, out int totalHits)
        {
            Collection<ScoreDocument> scoreDocuments = new Collection<ScoreDocument>();

            //Handle Categories and ACL in expression
            q = _commonFunc.PrepareExpression(q, excludeNotPublished);

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

        public void OptimizeIndex(NamedIndex namedIndex)
        {
            ReaderWriterLockSlim rwl = IndexingServiceSettings.ReaderWriterLocks[namedIndex.Name];

            rwl.EnterWriteLock();

            try
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start optimizing index"));

                IndexWriterConfig iwc = new IndexWriterConfig(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.Analyzer);
                iwc.OpenMode = OpenMode.CREATE_OR_APPEND;
                using (var iWriter = new IndexWriter(namedIndex.Directory, iwc))
                {
                    iWriter.ForceMerge(1);
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
            IndexingController.OnIndexedOptimized(this, new OptimizedEventArgs(namedIndex.Name));

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Optimized index: '{0}'", namedIndex.Name));
        }

        public bool DeleteFromIndex(NamedIndex namedIndex, string itemId, bool deleteRef)
        {
            ReaderWriterLockSlim rwl = IndexingServiceSettings.ReaderWriterLocks[namedIndex.Name];

            Term term = null;

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start deleting Lucene document with id field '{0}' from index '{1}'", itemId, namedIndex.Name));

            int i = 0;
            int pendingDeletions = 0;

            IndexWriterConfig iwc = new IndexWriterConfig(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.Analyzer);
            iwc.OpenMode = OpenMode.CREATE_OR_APPEND;

            rwl.EnterWriteLock();
            try
            {
                using (IndexWriter iWriter = new IndexWriter(namedIndex.Directory, iwc))
                {
                    using (var reader = DirectoryReader.Open(namedIndex.Directory))
                    {
                        term = new Term(IndexingServiceSettings.IdFieldName, itemId);
                        i = reader.NumDocs;
                        iWriter.DeleteDocuments(term);

                        pendingDeletions = reader.NumDeletedDocs;
                    }
                }
            }
            catch (Exception e)
            {
                _responseExceptionHelper.HandleServiceError(String.Format("Failed to delete Document with id: {0}. Message: {1}{2}{3}", itemId.ToString(), e.Message, Environment.NewLine, e.StackTrace));
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
                        using (IndexWriter iWriter = new IndexWriter(namedIndex.ReferenceDirectory, iwc))
                        {
                            Term refTerm = new Term(IndexingServiceSettings.ReferenceIdFieldName, itemId);
                            iWriter.DeleteDocuments(refTerm);
                        }
                    }
                    catch (Exception e)
                    {
                        _responseExceptionHelper.HandleServiceError(String.Format("Failed to delete referencing Documents for reference id: {0}. Message: {1}{2}{3}", itemId.ToString(), e.Message, Environment.NewLine, e.StackTrace));
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

        public void Update(FeedItemModel feedItem, NamedIndex namedIndex)
        {
            // Store old virtual path values if they are needed later to update virtual paths for subnodes
            string oldVirtualPath = String.Empty;
            string newVirtualPath = String.Empty;

            if (_feedHelper.GetAutoUpdateVirtualPathValue(feedItem))
            {
                Document doc = GetDocumentById(feedItem.Id, namedIndex);

                if (doc != null)
                {
                    oldVirtualPath = doc.Get(IndexingServiceSettings.VirtualPathFieldName);
                    newVirtualPath = new VirtualPathFieldStoreSerializer(feedItem).ToFieldStoreValue();
                }
            }

            Remove(feedItem.Id, namedIndex, false);
            Add(feedItem, namedIndex);

            UpdateVirtualPaths(oldVirtualPath, newVirtualPath);
        }

        public void UpdateVirtualPaths(string oldVirtualPath, string newVirtualPath)
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
                doc.Add(new TextField(IndexingServiceSettings.VirtualPathFieldName, vp,
                    IndexingServiceSettings.FieldProperties[IndexingServiceSettings.VirtualPathFieldName].FieldStore));

                AddAllSearchableContentsFieldToDocument(doc, namedIndex);

                // Remove and add the document
                Remove(id, namedIndex, false);
                WriteToIndex(id, doc, namedIndex);

                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Updated virtual path for document with id: '{0}'.", id));
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("End updating virtual paths"));
        }

        public FeedItemModel GetSyndicationItemFromDocument(ScoreDocument scoreDocument)
        {
            Document doc = scoreDocument.Document;

            // Create namedIndex for this document
            NamedIndex namedIndex = new NamedIndex(doc.Get(IndexingServiceSettings.NamedIndexFieldName));

            //Create search result object and add it to result collection
            FeedItemModel feedItem = new FeedItemModel();

            // ID field
            feedItem.Id = namedIndex.IncludeInResponse(IndexingServiceSettings.IdFieldName) ? doc.Get(IndexingServiceSettings.IdFieldName) : "";

            // Title field
            feedItem.Title = namedIndex.IncludeInResponse(IndexingServiceSettings.TitleFieldName) ? doc.Get(IndexingServiceSettings.TitleFieldName) : "";

            // DisplayText field
            feedItem.DisplayText = namedIndex.IncludeInResponse(IndexingServiceSettings.DisplayTextFieldName) ? doc.Get(IndexingServiceSettings.DisplayTextFieldName) : "";

            // Modified field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.ModifiedFieldName))
            {
                feedItem.Modified = new DateTimeOffset(Convert.ToDateTime(Regex.Replace(doc.Get(IndexingServiceSettings.ModifiedFieldName), @"(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})", "$1-$2-$3 $4:$5:$6"), CultureInfo.InvariantCulture));
            }

            // Created field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.CreatedFieldName))
            {
                feedItem.Created = new DateTimeOffset(Convert.ToDateTime(Regex.Replace(doc.Get(IndexingServiceSettings.CreatedFieldName), @"(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})", "$1-$2-$3 $4:$5:$6"), CultureInfo.InvariantCulture));
            }

            // Uri field
            Uri uri;
            if (Uri.TryCreate(doc.Get(IndexingServiceSettings.UriFieldName), UriKind.RelativeOrAbsolute, out uri))
            {
                feedItem.Uri = namedIndex.IncludeInResponse(IndexingServiceSettings.UriFieldName) ? uri : null;
            }

            // Culture field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.CultureFieldName))
            {
                feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameCulture, doc.Get(IndexingServiceSettings.CultureFieldName));
            }

            // ItemStatus field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.ItemStatusFieldName))
            {
                feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameItemStatus, doc.Get(IndexingServiceSettings.ItemStatusFieldName));
            }

            // Type field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.TypeFieldName))
            {
                feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameType, doc.Get(IndexingServiceSettings.TypeFieldName));
            }

            // Score field not optional. Always included
            feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameScore, scoreDocument.Score.ToString(CultureInfo.InvariantCulture));

            // Data Uri not optional. Always included
            feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameDataUri, doc.Get(IndexingServiceSettings.SyndicationItemAttributeNameDataUri));

            // Boost factor not optional. Always included
            feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameBoostFactor, doc.Fields[1].Boost.ToString(CultureInfo.InvariantCulture));

            // Named index not optional. Always included
            feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameNamedIndex, doc.Get(IndexingServiceSettings.NamedIndexFieldName));

            // PublicationEnd field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.PublicationEndFieldName))
            {
                string publicationEnd = doc.Get(IndexingServiceSettings.PublicationEndFieldName);
                if (!publicationEnd.Equals("no"))
                {
                    feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNamePublicationEnd, Convert.ToDateTime(Regex.Replace(publicationEnd, @"(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})", "$1-$2-$3 $4:$5:$6Z"), CultureInfo.InvariantCulture).ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
                }
            }

            // PublicationStart field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.PublicationStartFieldName))
            {
                string publicationStart = doc.Get(IndexingServiceSettings.PublicationStartFieldName);
                if (!publicationStart.Equals("no"))
                {
                    feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNamePublicationStart, Convert.ToDateTime(Regex.Replace(publicationStart, @"(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})", "$1-$2-$3 $4:$5:$6Z"), CultureInfo.InvariantCulture).ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
                }
            }

            //Metadata field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.MetadataFieldName))
            {
                feedItem.ElementExtensions.Add(IndexingServiceSettings.SyndicationItemElementNameMetadata, doc.Get(IndexingServiceSettings.MetadataFieldName));
            }

            // Categories
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.CategoriesFieldName))
            {
                string fieldStoreValue = doc.Get(IndexingServiceSettings.CategoriesFieldName);
                new CategoriesFieldStoreSerializer
                    (fieldStoreValue).
                    AddFieldStoreValueToSyndicationItem(feedItem);
            }

            //Authors 
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.AuthorsFieldName))
            {
                string fieldStoreValue = doc.Get(IndexingServiceSettings.AuthorStorageFieldName);
                new AuthorsFieldStoreSerializer
                    (fieldStoreValue).AddFieldStoreValueToSyndicationItem(feedItem);
            }

            //RACL to syndication item
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.AclFieldName))
            {
                string fieldStoreValue = doc.Get(IndexingServiceSettings.AclFieldName);
                new AclFieldStoreSerializer
                    (fieldStoreValue).AddFieldStoreValueToSyndicationItem(feedItem);
            }

            //Virtual path to syndication item
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.VirtualPathFieldName))
            {
                string fieldStoreValue = doc.Get(IndexingServiceSettings.VirtualPathFieldName);
                new VirtualPathFieldStoreSerializer
                    (fieldStoreValue).
                    AddFieldStoreValueToSyndicationItem(feedItem);
            }

            return feedItem;
        }

        public Lucene.Net.Store.Directory CreateIndex(string name, System.IO.DirectoryInfo directoryInfo)
        {
            Lucene.Net.Store.Directory dir = null;

            IndexingServiceSettings.ReaderWriterLocks[name].EnterWriteLock();

            try
            {
                //Create directory
                dir = Lucene.Net.Store.FSDirectory.Open(directoryInfo);
                //Create index
                IndexWriterConfig iwc = new IndexWriterConfig(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.Analyzer);
                iwc.OpenMode = OpenMode.CREATE;
                using (IndexWriter iWriter = new IndexWriter(dir, iwc))
                {
                }
            }
            catch (Exception e)
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Error(String.Format("Failed to create index for path: '{0}'. Message: {1}{2}'", directoryInfo.FullName, e.Message, e.StackTrace));
            }
            finally
            {
                IndexingServiceSettings.ReaderWriterLocks[name].ExitWriteLock();
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Created index for path: '{0}'", directoryInfo.FullName));

            return dir;
        }

        /// <summary>
        /// Search in the named index reference index for the itemId and returns the reference id for tha found item.
        /// This method is used to find out if the current item is a reference item when updating and removing the item
        /// </summary>
        /// <param name="itemId">The id of the item to search for</param>
        /// <param name="namedIndex">The Main index for which Reference index to search</param>
        /// <returns></returns>
        public string GetReferenceIdForItem(string itemId, NamedIndex namedIndex)
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

        /// <summary>
        /// Runs in async mode and handles when a DataUri is defined and the content should be grabbed from the uri rather than the item itself
        /// </summary>
        /// <param name="item">The <see cref="SyndicationItem"/> passed to the index</param>
        /// <param name="namedIndex">The <see cref="NamedIndex"/> to use</param>
        public void HandleDataUri(FeedItemModel item, NamedIndex namedIndex)
        {
            // Get the uri string
            string uriString = _feedHelper.GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameDataUri);

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Start processing data uri callback for uri '{0}'", uriString));

            Uri uri = null;

            // Try to parse the uri
            if (!Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out uri))
            {
                _responseExceptionHelper.HandleServiceError(String.Format("Data Uri callback failed. Uri '{0}' is not well formed", uriString));
                return;
            }

            string content = String.Empty;

            if (uri.IsFile)
            {
                if (!File.Exists(uri.LocalPath))
                {
                    _responseExceptionHelper.HandleServiceError(String.Format("File for uri '{0}' does not exist", uri.ToString()));
                    return;
                }

                content = _commonFunc.GetFileUriContent(uri);
            }
            else
            {
                content = _commonFunc.GetNonFileUriContent(uri);
            }

            if (String.IsNullOrEmpty(content))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Content for uri '{0}' is empty", uri.ToString()));
                content = String.Empty;
            }

            string displayTextOut = String.Empty;
            string metadataOut = String.Empty;

            string textContent = item.DisplayText;
            if (textContent != null && !String.IsNullOrEmpty(textContent))
            {
                _commonFunc.SplitDisplayTextToMetadata(textContent + " " + content,
                    _feedHelper.GetElementValue(item, IndexingServiceSettings.SyndicationItemElementNameMetadata),
                                    out displayTextOut, out metadataOut);
            }
            else
            {
                _commonFunc.SplitDisplayTextToMetadata(content,
                    _feedHelper.GetElementValue(item, IndexingServiceSettings.SyndicationItemElementNameMetadata),
                                    out displayTextOut, out metadataOut);
            }

            item.DisplayText = displayTextOut;
            _feedHelper.SetElementValue(item, IndexingServiceSettings.SyndicationItemElementNameMetadata, metadataOut);

            string requestType = _feedHelper.GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameIndexAction);
            if (requestType == "add")
            {
                Add(item, namedIndex);
            }
            else if (requestType == "update")
            {
                Update(item, namedIndex);
            }

            string referenceId = _feedHelper.GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameReferenceId);

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
    }
}

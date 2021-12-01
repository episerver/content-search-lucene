using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using EPiServer.Search.IndexingService.Controllers;
using EPiServer.Search.IndexingService.FieldSerializers;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Microsoft.Extensions.Logging;

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
        private readonly IDocumentHelper _documentHelper;
        public LuceneHelper(IFeedHelper feedHelper,
            IResponseExceptionHelper responseExceptionHelper,
            ICommonFunc commonFunc,
            IDocumentHelper documentHelper)
        {
            _feedHelper = feedHelper;
            _responseExceptionHelper = responseExceptionHelper;
            _commonFunc = commonFunc;
            _documentHelper = documentHelper;
        }

        public Document GetDocumentFromSyndicationItem(FeedItemModel feedItem, NamedIndex namedIndex)
        {
            var id = feedItem.Id;
            var authors = _feedHelper.PrepareAuthors(feedItem);
            var title = feedItem.Title;
            var displayText = feedItem.DisplayText;
            var created = (feedItem.Created.Year < 2) ? DateTime.Now : feedItem.Created.DateTime;
            var modified = (feedItem.Modified.Year < 2) ? DateTime.Now : feedItem.Modified.DateTime;
            var url = (feedItem.Uri != null) ? feedItem.Uri.ToString() : "";
            var boostFactor = _feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNameBoostFactor);
            var culture = _feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNameCulture);
            var type = _feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNameType);
            var referenceId = _feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNameReferenceId);
            var metadata = _feedHelper.GetElementValue(feedItem, IndexingServiceSettings.SyndicationItemElementNameMetadata);
            var itemStatus = _feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNameItemStatus);

            var hasExpiration = false;
            if (DateTime.TryParse(_feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNamePublicationEnd), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var publicationEnd))
            {
                hasExpiration = true;
            }

            var hasStart = false;
            if (DateTime.TryParse(_feedHelper.GetAttributeValue(feedItem, IndexingServiceSettings.SyndicationItemAttributeNamePublicationStart), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var publicationStart))
            {
                hasStart = true;
            }

            var categoriesSerializer = new CategoriesFieldStoreSerializer(feedItem);
            var aclSerializer = new AclFieldStoreSerializer(feedItem);
            var virtualPathSerializer = new VirtualPathFieldStoreSerializer(feedItem);
            var authorsSerializer = new AuthorsFieldStoreSerializer(feedItem);

            // Split displayText
            var displayTextOut = string.Empty;
            var metadataOut = string.Empty;
            _commonFunc.SplitDisplayTextToMetadata(displayText, metadata, out displayTextOut, out metadataOut);

            //Create the document
            var doc = new Document
            {
                new StringField(IndexingServiceSettings.IdFieldName, id,
                Field.Store.YES),

                new TextField(IndexingServiceSettings.TitleFieldName, title,
                Field.Store.YES),

                new TextField(IndexingServiceSettings.DisplayTextFieldName, displayTextOut,
                Field.Store.YES),

                new StringField(IndexingServiceSettings.CreatedFieldName, Regex.Replace(created.ToString("u", CultureInfo.InvariantCulture), @"\D", ""),
                Field.Store.YES),

                new StringField(IndexingServiceSettings.ModifiedFieldName, Regex.Replace(modified.ToString("u", CultureInfo.InvariantCulture), @"\D", ""),
                Field.Store.YES),

                new StringField(IndexingServiceSettings.PublicationEndFieldName, hasExpiration ? Regex.Replace(publicationEnd.ToUniversalTime().ToString("u"), @"\D", "") : "no",
                Field.Store.YES),

                new StringField(IndexingServiceSettings.PublicationStartFieldName, hasStart ? Regex.Replace(publicationStart.ToUniversalTime().ToString("u"), @"\D", "") : "no",
                Field.Store.YES),

                new StringField(IndexingServiceSettings.UriFieldName, url,
                Field.Store.YES),

                new StringField(IndexingServiceSettings.MetadataFieldName, metadataOut,
                Field.Store.YES),

                new TextField(IndexingServiceSettings.CategoriesFieldName, categoriesSerializer.ToFieldStoreValue(),
                Field.Store.YES),

                new StringField(IndexingServiceSettings.CultureFieldName, culture,
                Field.Store.YES),

                new TextField(IndexingServiceSettings.AuthorsFieldName, authors,
                Field.Store.YES),

                new TextField(IndexingServiceSettings.TypeFieldName, type,
                Field.Store.YES),

                new StringField(IndexingServiceSettings.ReferenceIdFieldName, referenceId,
                Field.Store.YES),

                new TextField(IndexingServiceSettings.AclFieldName, aclSerializer.ToFieldStoreValue(),
                Field.Store.YES),

                new TextField(IndexingServiceSettings.VirtualPathFieldName, virtualPathSerializer.ToFieldStoreValue(),
                Field.Store.YES),

                new StringField(IndexingServiceSettings.AuthorStorageFieldName, authorsSerializer.ToFieldStoreValue(),
                Field.Store.YES),

                new StringField(IndexingServiceSettings.NamedIndexFieldName, namedIndex.Name,
                Field.Store.YES),

                new StringField(IndexingServiceSettings.ItemStatusFieldName, itemStatus,
                Field.Store.YES)
            };

            AddAllSearchableContentsFieldToDocument(doc, namedIndex);

            ((TextField)doc.Fields[1]).Boost = ((float.TryParse(boostFactor, out var fltBoostFactor)) ? fltBoostFactor : 1);

            return doc;
        }

        public void AddAllSearchableContentsFieldToDocument(Document doc, NamedIndex namedIndex)
        {
            var id = doc.Get(IndexingServiceSettings.IdFieldName);

            var totalContents = new StringBuilder();
            totalContents.Append(doc.Get(IndexingServiceSettings.TitleFieldName));
            totalContents.Append(" ");
            totalContents.Append(doc.Get(IndexingServiceSettings.DisplayTextFieldName));
            totalContents.Append(" ");
            totalContents.Append(doc.Get(IndexingServiceSettings.MetadataFieldName));
            totalContents.Append(" ");
            totalContents.Append(GetReferenceData(id, namedIndex));

            doc.RemoveField(IndexingServiceSettings.DefaultFieldName);
            doc.Add(new TextField(IndexingServiceSettings.DefaultFieldName, totalContents.ToString(), Field.Store.YES));
        }

        public string GetReferenceData(string referenceId, NamedIndex namedIndex)
        {
            if (namedIndex.ReferenceDirectory == null)
            {
                // This is a parent item
                return string.Empty;
            }

            var sb = new StringBuilder();

            try
            {
                namedIndex = new NamedIndex(namedIndex.Name, true);
                var scoreDocuments =
                    _documentHelper.SingleIndexSearch(string.Format("{0}:{1}",
                    IndexingServiceSettings.ReferenceIdFieldName, QueryParser.Escape(referenceId)), namedIndex, IndexingServiceSettings.MaxHitsForReferenceSearch, out var totalHits);

                foreach (var scoreDocument in scoreDocuments)
                {
                    var hitDoc = scoreDocument.Document;
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
                _responseExceptionHelper.HandleServiceError(string.Format("Could not get reference data for id: {0}. Message: {1}{2}{3}", referenceId, e.Message, Environment.NewLine, e.StackTrace));
                return null;
            }

            return sb.ToString();
        }

        public bool UpdateReference(string referenceId, string itemId, NamedIndex mainNamedIndex)
        {
            var mainDoc = _documentHelper.GetDocumentById(referenceId, mainNamedIndex);

            if (mainDoc == null)
            {
                IndexingServiceSettings.IndexingServiceServiceLog.LogError(string.Format("Could not find main document with id: '{0}' for referencing item id '{1}'. Continuing anyway, index will heal when main document is added/updated.", referenceId, itemId));
                return false;
            }

            AddAllSearchableContentsFieldToDocument(mainDoc, mainNamedIndex);

            //remove old parent document without removing its reference data
            Remove(referenceId, mainNamedIndex, false);
            // Add the man document again
            WriteToIndex(referenceId, mainDoc, mainNamedIndex);
            return true;
        }

        public bool WriteToIndex(string itemId, Document doc, NamedIndex namedIndex)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Start writing document with id '{0}' to index '{1}' with analyzer '{2}'", itemId, namedIndex.Name, IndexingServiceSettings.Analyzer.ToString()));

            // Write to Directory
            if (_documentHelper.DocumentExists(itemId, namedIndex))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Failed to write to index: '{0}'. Document with id: '{1}' already exists", namedIndex.Name, itemId));
                return false;
            }

            var rwl = new ReaderWriterLockSlim();

            rwl.EnterWriteLock();

            try
            {
                var iwc = new IndexWriterConfig(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.Analyzer)
                {
                    OpenMode = OpenMode.CREATE_OR_APPEND
                };
                using (var iWriter = new IndexWriter(namedIndex.Directory, iwc))
                {
                    iWriter.AddDocument(doc);
                }
            }
            catch (Exception e)
            {
                _responseExceptionHelper.HandleServiceError(string.Format("Failed to write to index: '{0}'. Message: {1}{2}{3}", namedIndex.Name, e.Message, Environment.NewLine, e.StackTrace));
                return false;
            }
            finally
            {
                rwl.ExitWriteLock();
            }

            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("End writing to index"));
            return true;
        }

        public bool Add(FeedItemModel item, NamedIndex namedIndex)
        {
            if (item == null)
            {
                return false;
            }

            var id = item.Id;

            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Start adding Lucene document with id field: '{0}' to index: '{1}'", id, namedIndex.Name));

            if (string.IsNullOrEmpty(id))
            {
                _responseExceptionHelper.HandleServiceError(string.Format("Failed to add Document. id field is missing."));
                return false;
            }

            //Check if the document already exists.
            if (_documentHelper.DocumentExists(id, namedIndex))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Document already exists. Skipping."));
                return false;
            }

            var doc = GetDocumentFromSyndicationItem(item, namedIndex);

            //Fire adding event
            IndexingController.OnDocumentAdding(this, new AddUpdateEventArgs(doc, namedIndex.Name));

            var result = WriteToIndex(id, doc, namedIndex);

            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("End adding document with id field: '{0}' to index: '{1}'", id, namedIndex.Name));

            //Fire added event
            IndexingController.OnDocumentAdded(this, new AddUpdateEventArgs(doc, namedIndex.Name));

            return result;
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
            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Start deleting Lucene document with id field: '{0}' from index '{1}'", itemId, namedIndex.Name));

            IndexingController.OnDocumentRemoving(this, new RemoveEventArgs(itemId, namedIndex.Name));

            var succeeded = DeleteFromIndex(namedIndex, itemId, removeRef);

            if (succeeded)
            {
                IndexingController.OnDocumentRemoved(this, new RemoveEventArgs(itemId, namedIndex.Name));

                IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("End deleting document with id field: '{0}'", itemId));
            }
        }

        public bool RemoveByVirtualPath(string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath))
            {
                return false;
            }

            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Start removing all items under virtual path '{0}'.", virtualPath));

            //Get all documents under old virtual path for all named indexes
            var allNamedIndexes = new Collection<NamedIndex>();
            foreach (var name in IndexingServiceSettings.NamedIndexElements.Keys)
            {
                allNamedIndexes.Add(new NamedIndex(name));
            }

            var scoreDocuments =
                GetScoreDocuments(string.Format("{0}:{1}*", IndexingServiceSettings.VirtualPathFieldName, virtualPath),
                false, allNamedIndexes, 0, IndexingServiceSettings.MaxHitsForReferenceSearch,
                IndexingServiceSettings.MaxHitsForReferenceSearch, out var totalHits);

            foreach (var scoreDocument in scoreDocuments)
            {
                var doc = scoreDocument.Document;
                var namedIndex = new NamedIndex(doc.Get(IndexingServiceSettings.NamedIndexFieldName));
                var id = doc.Get(IndexingServiceSettings.IdFieldName);

                Remove(id, namedIndex, true);
            }

            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("End removing by virtual path."));
            return true;
        }

        public Collection<ScoreDocument> GetScoreDocuments(string q, bool excludeNotPublished, Collection<NamedIndex> namedIndexes, int offset, int limit, int maxHits, out int totalHits)
        {
            var scoreDocuments = new Collection<ScoreDocument>();

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
                scoreDocuments = _documentHelper.SingleIndexSearch(q, namedIndexes[0], maxHits, out totalHits);
            }
            else
            {
                scoreDocuments = _documentHelper.MultiIndexSearch(q, namedIndexes, maxHits, out totalHits);
            }

            var results = new Collection<ScoreDocument>();
            var hitsToTake = limit + offset;
            hitsToTake = (totalHits < hitsToTake ? totalHits : hitsToTake);
            for (var i = offset; i < hitsToTake; i++)
            {
                results.Add(scoreDocuments[i]);
            }

            return results;
        }

        public bool DeleteFromIndex(NamedIndex namedIndex, string itemId, bool deleteRef)
        {
            var rwl = new ReaderWriterLockSlim();

            Term term = null;

            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Start deleting Lucene document with id field '{0}' from index '{1}'", itemId, namedIndex.Name));

            var i = 0;
            var pendingDeletions = 0;

            rwl.EnterWriteLock();
            try
            {
                var iwc = new IndexWriterConfig(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.Analyzer)
                {
                    OpenMode = OpenMode.CREATE_OR_APPEND
                };
                using (var iWriter = new IndexWriter(namedIndex.Directory, iwc))
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
                _responseExceptionHelper.HandleServiceError(string.Format("Failed to delete Document with id: {0}. Message: {1}{2}{3}", itemId.ToString(), e.Message, Environment.NewLine, e.StackTrace));
                return false;
            }
            finally
            {
                rwl.ExitWriteLock();
            }

            if (i == 0) // Document didn't exist
            {
                IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Failed to delete Document with id: {0}. Document does not exist.", itemId.ToString()));
                return false;
            }
            else
            {
                // Delete any referencing documents
                if (deleteRef && namedIndex.ReferenceDirectory != null)
                {
                    IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Start deleting reference documents for id '{0}'", itemId.ToString()));

                    var rwlRef = new ReaderWriterLockSlim();
                    rwlRef.EnterWriteLock();

                    try
                    {
                        var iwc = new IndexWriterConfig(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.Analyzer)
                        {
                            OpenMode = OpenMode.CREATE_OR_APPEND
                        };
                        using (var iWriter = new IndexWriter(namedIndex.ReferenceDirectory, iwc))
                        {
                            var refTerm = new Term(IndexingServiceSettings.ReferenceIdFieldName, itemId);
                            iWriter.DeleteDocuments(refTerm);
                        }
                    }
                    catch (Exception e)
                    {
                        _responseExceptionHelper.HandleServiceError(string.Format("Failed to delete referencing Documents for reference id: {0}. Message: {1}{2}{3}", itemId.ToString(), e.Message, Environment.NewLine, e.StackTrace));
                        return false;
                    }
                    finally
                    {
                        rwlRef.ExitWriteLock();
                    }

                    IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("End deleting reference documents for id '{0}'", itemId.ToString()));
                }

                IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("End deleting Lucene document with id field: '{0}'", itemId));

                // Optimize index
                if ((namedIndex.PendingDeletesOptimizeThreshold > 0) &&
                    (pendingDeletions >= namedIndex.PendingDeletesOptimizeThreshold))
                {
                    _documentHelper.OptimizeIndex(namedIndex);
                }

                return true;
            }
        }

        public void Update(FeedItemModel feedItem, NamedIndex namedIndex)
        {
            // Store old virtual path values if they are needed later to update virtual paths for subnodes
            var oldVirtualPath = string.Empty;
            var newVirtualPath = string.Empty;

            if (_feedHelper.GetAutoUpdateVirtualPathValue(feedItem))
            {
                var doc = _documentHelper.GetDocumentById(feedItem.Id, namedIndex);

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

        public bool UpdateVirtualPaths(string oldVirtualPath, string newVirtualPath)
        {
            if (string.IsNullOrEmpty(newVirtualPath) || newVirtualPath.Equals(oldVirtualPath, StringComparison.InvariantCulture))
            {
                return false;
            }

            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Start updating virtual paths from old path: '{0}' to new path '{1}'", oldVirtualPath, newVirtualPath));

            //Get all documents under old virtual path for all named indexes
            var allNamedIndexes = new Collection<NamedIndex>();
            foreach (var name in IndexingServiceSettings.NamedIndexElements.Keys)
            {
                allNamedIndexes.Add(new NamedIndex(name));
            }

            var scoreDocuments =
                GetScoreDocuments(string.Format("{0}:{1}*", IndexingServiceSettings.VirtualPathFieldName, oldVirtualPath),
                false, allNamedIndexes, 0, IndexingServiceSettings.MaxHitsForReferenceSearch,
                IndexingServiceSettings.MaxHitsForReferenceSearch, out var totalHits);

            foreach (var scoreDocument in scoreDocuments)
            {
                var doc = scoreDocument.Document;
                var namedIndex = new NamedIndex(doc.Get(IndexingServiceSettings.NamedIndexFieldName));

                var id = doc.Get(IndexingServiceSettings.IdFieldName);
                var vp = doc.Get(IndexingServiceSettings.VirtualPathFieldName);
                vp = vp.Remove(0, oldVirtualPath.Length);
                vp = vp.Insert(0, newVirtualPath);
                doc.RemoveField(IndexingServiceSettings.VirtualPathFieldName);
                doc.Add(new TextField(IndexingServiceSettings.VirtualPathFieldName, vp,
                    Field.Store.YES));

                AddAllSearchableContentsFieldToDocument(doc, namedIndex);

                // Remove and add the document
                Remove(id, namedIndex, false);
                WriteToIndex(id, doc, namedIndex);

                IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Updated virtual path for document with id: '{0}'.", id));
            }

            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("End updating virtual paths"));
            return true;
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
            var refNamedIndex = new NamedIndex(namedIndex.Name, true);

            var doc = _documentHelper.GetDocumentById(itemId, refNamedIndex);
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
        public bool HandleDataUri(FeedItemModel item, NamedIndex namedIndex)
        {
            // Get the uri string
            var uriString = _feedHelper.GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameDataUri);

            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Start processing data uri callback for uri '{0}'", uriString));


            // Try to parse the uri
            if (!Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var uri))
            {
                _responseExceptionHelper.HandleServiceError(string.Format("Data Uri callback failed. Uri '{0}' is not well formed", uriString));
                return false;
            }

            var content = string.Empty;

            if (uri.IsFile)
            {
                if (!File.Exists(uri.LocalPath))
                {
                    _responseExceptionHelper.HandleServiceError(string.Format("File for uri '{0}' does not exist", uri.ToString()));
                    return false;
                }

                content = _commonFunc.GetFileUriContent(uri);
            }
            else
            {
                content = _commonFunc.GetNonFileUriContent(uri);
            }

            if (string.IsNullOrEmpty(content))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Content for uri '{0}' is empty", uri.ToString()));
                content = string.Empty;
            }

            var displayTextOut = string.Empty;
            var metadataOut = string.Empty;

            var textContent = item.DisplayText;
            if (textContent != null && !string.IsNullOrEmpty(textContent))
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

            var requestType = _feedHelper.GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameIndexAction);
            if (requestType == "add")
            {
                Add(item, namedIndex);
            }
            else if (requestType == "update")
            {
                Update(item, namedIndex);
            }

            var referenceId = _feedHelper.GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameReferenceId);

            // If this item is a reference item we need to update the parent document to 
            // reflect changes in the reference index. e.g. comments.
            if (!string.IsNullOrEmpty(referenceId))
            {
                UpdateReference(
                    referenceId,
                    item.Id,
                    new NamedIndex(namedIndex.Name)); //Always main index

                IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Updated reference with reference id '{0}' ", referenceId));
            }

            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("End data uri callback"));
            return true;
        }
    }
}

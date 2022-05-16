using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using EPiServer.Search.IndexingService.Controllers;
using EPiServer.Search.IndexingService.FieldSerializers;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Microsoft.Extensions.Logging;

namespace EPiServer.Search.IndexingService.Helpers
{
    public class DocumentHelper : IDocumentHelper
    {
        private readonly IResponseExceptionHelper _responseExceptionHelper;
        private readonly ILogger<DocumentHelper> _logger;
        public DocumentHelper(IResponseExceptionHelper responseExceptionHelper, ILogger<DocumentHelper> logger)
        {
            _responseExceptionHelper = responseExceptionHelper;
            _logger = logger;
        }
        public Collection<ScoreDocument> SingleIndexSearch(string q, NamedIndex namedIndex, int maxHits, out int totalHits)
        {
            var scoreDocuments = new Collection<ScoreDocument>();
            totalHits = 0;
            var rwl = IndexingServiceSettings.ReaderWriterLocks[namedIndex.Name];
            rwl.EnterReadLock();

            try
            {
                _logger.LogDebug(string.Format("Creating Lucene QueryParser for index '{0}' with expression '{1}' with analyzer '{2}'", namedIndex.Name, q, IndexingServiceSettings.Analyzer.ToString()));
                QueryParser parser = new PerFieldQueryParserWrapper(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.DefaultFieldName, IndexingServiceSettings.Analyzer, IndexingServiceSettings.LowercaseFields);
                var baseQuery = parser.Parse(q);
                using (IndexReader reader = DirectoryReader.Open(namedIndex.Directory))
                {
                    var searcher = new IndexSearcher(reader);

                    var topDocs = searcher.Search(baseQuery, maxHits);
                    totalHits = topDocs.TotalHits;
                    var docs = topDocs.ScoreDocs;
                    for (var i = 0; i < docs.Length; i++)
                    {
                        scoreDocuments.Add(new ScoreDocument(searcher.Doc(docs[i].Doc), docs[i].Score));
                    }
                }
            }
            catch (Exception e)
            {
                _responseExceptionHelper.HandleServiceError(string.Format("Failed to search index '{0}'. Index seems to be corrupt! Message: {1}{2}{3}", namedIndex.Name, e.Message, Environment.NewLine, e.StackTrace));
            }
            finally
            {
                rwl.ExitReadLock();
            }

            return scoreDocuments;
        }
        public void OptimizeIndex(NamedIndex namedIndex)
        {
            var rwl = IndexingServiceSettings.ReaderWriterLocks[namedIndex.Name];

            rwl.EnterWriteLock();

            try
            {
                _logger.LogDebug(string.Format("Start optimizing index"));

                var iwc = new IndexWriterConfig(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.Analyzer)
                {
                    OpenMode = OpenMode.CREATE_OR_APPEND
                };
                using (var iWriter = new IndexWriter(namedIndex.Directory, iwc))
                {
                    iWriter.ForceMerge(1);
                }

                _logger.LogDebug(string.Format("End optimizing index"));

            }
            catch (Exception e)
            {
                _logger.LogError(string.Format("Failed to optimize index: '{0}'. Message: {1}{2}{3}", namedIndex.Name, e.Message, Environment.NewLine, e.StackTrace));
            }
            finally
            {
                rwl.ExitWriteLock();
            }

            //Fire event
            IndexingController.OnIndexedOptimized(this, new OptimizedEventArgs(namedIndex.Name));

            _logger.LogDebug(string.Format("Optimized index: '{0}'", namedIndex.Name));
        }
        public Collection<ScoreDocument> MultiIndexSearch(string q, Collection<NamedIndex> namedIndexes, int maxHits, out int totalHits)
        {
            //Prepare queries for MultiSearcher
            var defaultFieldName = IndexingServiceSettings.DefaultFieldName;
            var readers = new IndexReader[namedIndexes.Count];
            var locks = new Collection<ReaderWriterLockSlim>();

            //Modify queries for other indexes with other field names
            var i = 0;
            foreach (var namedIndex in namedIndexes)
            {
                var rwl = IndexingServiceSettings.ReaderWriterLocks[namedIndex.Name];
                locks.Add(rwl);
                rwl.EnterReadLock();

                try
                {
                    _logger.LogDebug(string.Format("Creating Lucene QueryParser for index '{0}' with expression '{1}' with analyzer '{2}'", namedIndex.Name, q, IndexingServiceSettings.Analyzer.ToString()));
                    readers[i] = DirectoryReader.Open(namedIndex.Directory);
                }
                catch (Exception e)
                {
                    _responseExceptionHelper.HandleServiceError(string.Format("Failed to create sub searcher for index '{0}' Message: {1}{2}{3}", namedIndex.Name, e.Message, Environment.NewLine, e.StackTrace));
                }
                finally
                {
                    rwl.ExitReadLock();
                }

                i++;
            }

            QueryParser parser = new PerFieldQueryParserWrapper(IndexingServiceSettings.LuceneVersion, defaultFieldName, IndexingServiceSettings.Analyzer, IndexingServiceSettings.LowercaseFields);
            var query = parser.Parse(q);
            var scoreDocuments = new Collection<ScoreDocument>();
            totalHits = 0;

            // Read locks
            foreach (var rwl in locks)
            {
                rwl.EnterReadLock();
            }

            try
            {
                using (var multiReader = new MultiReader(readers))
                {
                    var searcher = new IndexSearcher(multiReader);
                    var topDocs = searcher.Search(query, maxHits);
                    totalHits = topDocs.TotalHits;
                    var docs = topDocs.ScoreDocs;
                    for (var j = 0; j < docs.Length; j++)
                    {
                        scoreDocuments.Add(new ScoreDocument(searcher.Doc(docs[j].Doc), docs[j].Score));
                    }
                }
            }
            catch (Exception e)
            {
                _responseExceptionHelper.HandleServiceError(string.Format("Failed to get hits from MultiSearcher! Message: {0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace));
            }
            finally
            {
                foreach (var rwl in locks)
                {
                    rwl.ExitReadLock();
                }
            }

            return scoreDocuments;
        }
        public FeedItemModel GetSyndicationItemFromDocument(ScoreDocument scoreDocument)
        {
            var doc = scoreDocument.Document;

            // Create namedIndex for this document
            var namedIndex = new NamedIndex(doc.Get(IndexingServiceSettings.NamedIndexFieldName));

            //Create search result object and add it to result collection
            var feedItem = new FeedItemModel
            {

                // ID field
                Id = namedIndex.IncludeInResponse(IndexingServiceSettings.IdFieldName) ? doc.Get(IndexingServiceSettings.IdFieldName) : "",

                // Title field
                Title = namedIndex.IncludeInResponse(IndexingServiceSettings.TitleFieldName) ? doc.Get(IndexingServiceSettings.TitleFieldName) : "",

                // DisplayText field
                DisplayText = namedIndex.IncludeInResponse(IndexingServiceSettings.DisplayTextFieldName) ? doc.Get(IndexingServiceSettings.DisplayTextFieldName) : ""
            };

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
            if (Uri.TryCreate(doc.Get(IndexingServiceSettings.UriFieldName), UriKind.RelativeOrAbsolute, out var uri))
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
                var publicationEnd = doc.Get(IndexingServiceSettings.PublicationEndFieldName);
                if (!publicationEnd.Equals("no"))
                {
                    feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNamePublicationEnd, Convert.ToDateTime(Regex.Replace(publicationEnd, @"(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})", "$1-$2-$3 $4:$5:$6Z"), CultureInfo.InvariantCulture).ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
                }
            }

            // PublicationStart field
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.PublicationStartFieldName))
            {
                var publicationStart = doc.Get(IndexingServiceSettings.PublicationStartFieldName);
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
                var fieldStoreValue = doc.Get(IndexingServiceSettings.CategoriesFieldName);
                new CategoriesFieldStoreSerializer
                    (fieldStoreValue).
                    AddFieldStoreValueToSyndicationItem(feedItem);
            }

            //Authors 
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.AuthorsFieldName))
            {
                var fieldStoreValue = doc.Get(IndexingServiceSettings.AuthorStorageFieldName);
                new AuthorsFieldStoreSerializer
                    (fieldStoreValue).AddFieldStoreValueToSyndicationItem(feedItem);
            }

            //RACL to syndication item
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.AclFieldName))
            {
                var fieldStoreValue = doc.Get(IndexingServiceSettings.AclFieldName);
                new AclFieldStoreSerializer
                    (fieldStoreValue).AddFieldStoreValueToSyndicationItem(feedItem);
            }

            //Virtual path to syndication item
            if (namedIndex.IncludeInResponse(IndexingServiceSettings.VirtualPathFieldName))
            {
                var fieldStoreValue = doc.Get(IndexingServiceSettings.VirtualPathFieldName);
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
                var iwc = new IndexWriterConfig(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.Analyzer)
                {
                    OpenMode = OpenMode.CREATE
                };
                using (var iWriter = new IndexWriter(dir, iwc))
                {
                }
            }
            catch (Exception e)
            {
                _logger.LogError(string.Format("Failed to create index for path: '{0}'. Message: {1}{2}'", directoryInfo.FullName, e.Message, e.StackTrace));
            }
            finally
            {
                IndexingServiceSettings.ReaderWriterLocks[name].ExitWriteLock();
            }

            _logger.LogDebug(string.Format("Created index for path: '{0}'", directoryInfo.FullName));

            return dir;
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
                var docs = SingleIndexSearch(string.Format("{0}:{1}",
                    IndexingServiceSettings.IdFieldName,
                    QueryParser.Escape(itemId)), namedIndex, 1, out var totalHits);

                if (docs.Count > 0)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                _responseExceptionHelper.HandleServiceError(string.Format("Could not verify document existense for id: '{0}'. Message: {1}{2}{3}", itemId, e.Message, Environment.NewLine, e.StackTrace));
            }
            return false;
        }
        public Document GetDocumentById(string id, NamedIndex namedIndex)
        {
            var docs = SingleIndexSearch(string.Format("{0}:{1}",
                IndexingServiceSettings.IdFieldName,
                QueryParser.Escape(id)), namedIndex, 1, out var totalHits);

            if (docs.Count > 0)
            {
                return docs[0].Document;
            }
            else
            {
                return null;
            }
        }
    }
}

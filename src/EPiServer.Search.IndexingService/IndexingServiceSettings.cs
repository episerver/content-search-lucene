using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.Threading;
using EPiServer.Logging.Compatibility;
using EPiServer.Search.IndexingService.Configuration;
using EPiServer.Search.IndexingService.Controllers;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Microsoft.Extensions.Options;

namespace EPiServer.Search.IndexingService
{
    internal class IndexingServiceSettings : IIndexingServiceSettings
    {
        #region Member variables
        internal const string TagsPrefix = "[[";
        internal const string TagsSuffix = "]]";

        internal const string DefaultFieldName = "EPISERVER_SEARCH_DEFAULT";
        internal const string IdFieldName = "EPISERVER_SEARCH_ID";
        internal const string TitleFieldName = "EPISERVER_SEARCH_TITLE";
        internal const string DisplayTextFieldName = "EPISERVER_SEARCH_DISPLAYTEXT";
        internal const string CreatedFieldName = "EPISERVER_SEARCH_CREATED";
        internal const string ModifiedFieldName = "EPISERVER_SEARCH_MODIFIED";
        internal const string PublicationEndFieldName = "EPISERVER_SEARCH_PUBLICATIONEND";
        internal const string PublicationStartFieldName = "EPISERVER_SEARCH_PUBLICATIONSTART";
        internal const string UriFieldName = "EPISERVER_SEARCH_URI";
        internal const string CategoriesFieldName = "EPISERVER_SEARCH_CATEGORIES";
        internal const string AuthorsFieldName = "EPISERVER_SEARCH_AUTHORS";
        internal const string CultureFieldName = "EPISERVER_SEARCH_CULTURE";
        internal const string TypeFieldName = "EPISERVER_SEARCH_TYPE";
        internal const string ReferenceIdFieldName = "EPISERVER_SEARCH_REFERENCEID";
        internal const string MetadataFieldName = "EPISERVER_SEARCH_METADATA";
        internal const string AclFieldName = "EPISERVER_SEARCH_ACL";
        internal const string VirtualPathFieldName = "EPISERVER_SEARCH_VIRTUALPATH";
        internal const string DataUriFieldName = "EPISERVER_SEARCH_DATAURI";
        internal const string AuthorStorageFieldName = "EPISERVER_SEARCH_AUTHORSTORAGE";
        internal const string NamedIndexFieldName = "EPISERVER_SEARCH_NAMEDINDEX";
        internal const string ItemStatusFieldName = "EPISERVER_SEARCH_ITEMSTATUS";

        internal const string XmlQualifiedNamespace = "EPiServer.Search.IndexingService";
        internal const string SyndicationItemAttributeNameCulture = "Culture";
        internal const string SyndicationItemAttributeNameType = "Type";
        internal const string SyndicationItemElementNameMetadata = "Metadata";
        internal const string SyndicationItemAttributeNameNamedIndex = "NamedIndex";
        internal const string SyndicationItemAttributeNameBoostFactor = "BoostFactor";
        internal const string SyndicationItemAttributeNameIndexAction = "IndexAction";
        internal const string SyndicationItemAttributeNameReferenceId = "ReferenceId";
        internal const string SyndicationItemElementNameAcl = "ACL";
        internal const string SyndicationItemAttributeNameDataUri = "DataUri";
        internal const string SyndicationItemElementNameVirtualPath = "VirtualPath";
        internal const string SyndicationItemAttributeNameScore = "Score";
        internal const string SyndicationFeedAttributeNameVersion = "Version";
        internal const string SyndicationFeedAttributeNameTotalHits = "TotalHits";
        internal const string SyndicationItemAttributeNamePublicationEnd = "PublicationEnd";
        internal const string SyndicationItemAttributeNamePublicationStart = "PublicationStart";
        internal const string SyndicationItemAttributeNameItemStatus = "ItemStatus";
        internal const string SyndicationItemAttributeNameAutoUpdateVirtualPath = "AutoUpdateVirtualPath";

        internal const string RefIndexSuffix = "_ref";

        private static string _defaultIndexName;
        private static Analyzer _analyzer;
        private static Dictionary<string, ClientElement> _clientElements = new Dictionary<string, ClientElement>();
        private static Dictionary<string, NamedIndexElement> _namedIndexElements = new Dictionary<string, NamedIndexElement>();
        private static Dictionary<string, Directory> _namedIndexDirectories = new Dictionary<string, Directory>();
        private static Dictionary<string, Directory> _referenceIndexDirectories = new Dictionary<string, Directory>();
        private static Dictionary<string, System.IO.DirectoryInfo> _mainDirectoryInfos = new Dictionary<string, System.IO.DirectoryInfo>();
        private static Dictionary<string, System.IO.DirectoryInfo> _referenceDirectoryInfos = new Dictionary<string, System.IO.DirectoryInfo>();
        private static Dictionary<string, int> _indexWriteCounters = new Dictionary<string, int>();
        private static Dictionary<string, Analyzer> _indexAnalyzers = new Dictionary<string, Analyzer>();
        private static Dictionary<string, ReaderWriterLockSlim> _readerWriterLocks = new Dictionary<string, ReaderWriterLockSlim>();
        private static Dictionary<string, FieldProperties> _fieldProperties = new Dictionary<string, FieldProperties>();
        private static IList<string> _lowercaseFields = new List<string>() { DefaultFieldName, TitleFieldName, DisplayTextFieldName, AuthorsFieldName };

        private readonly IndexingServiceOptions _indexingServiceOpts;
        private readonly IIndexingServiceHandler _indexingServiceHandler;
        #endregion

        #region Construct and Init

        public IndexingServiceSettings(IOptions<IndexingServiceOptions> indexingServiceOpts,
             IIndexingServiceHandler indexingServiceHandler)
        {
            _indexingServiceOpts = indexingServiceOpts.Value;
            _indexingServiceHandler = indexingServiceHandler;

            Init();
        }

        public void Dispose()
        {
        }

        private void Init()
        {
            //Start logging
            IndexingServiceServiceLog = LogManager.GetLogger(typeof(IndexingController));

            //Must check breaking changes and database format compatibility before 'upgrading' this
            LuceneVersion = Lucene.Net.Util.LuceneVersion.LUCENE_48;

            //Load configuration
            LoadConfiguration();

            //Create or load named indexes
            LoadIndexes();

            LoadFieldProperties();

            LoadAnalyzer();

            IndexingServiceServiceLog.Info("EPiServer Indexing Service Started!");
        }

        #endregion

        #region Internal properties

        internal static Lucene.Net.Util.LuceneVersion LuceneVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the maximum number of characters to add to document display text field 
        /// The rest is added to the document meta data field
        /// </summary>
        internal static int MaxDisplayTextLength
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the FIPSCompliant 
        /// </summary>
        public static bool FIPSCompliant { get; private set; }

        /// <summary>
        /// Gets and sets the maximum number of hits to be returned by a Lucene Search. Overrides the passed maxitems.
        /// </summary>
        internal static int MaxHitsForSearchResults
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the maximum number of hits to be returned when doing a search in reference index. e.g. how many top comments should be included in the parent documents metadata
        /// </summary>
        internal static int MaxHitsForReferenceSearch
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the default Lucene Directory
        /// </summary>
        internal static Directory DefaultDirectory
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the default Lucene reference Directory
        /// </summary>
        internal static Directory DefaultReferenceDirectory
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets and sets the Log4Net logger
        /// </summary>
        public static ILog IndexingServiceServiceLog { get; set; }

        internal static Analyzer Analyzer
        {
            get
            {
                return _analyzer;
            }
        }

        /// <summary>
        /// Gets ReaderWriterLocks for named indexes
        /// </summary>
        internal static Dictionary<string, ReaderWriterLockSlim> ReaderWriterLocks
        {
            get
            {
                return _readerWriterLocks;
            }
        }

        /// <summary>
        /// Gets named indexes config elements
        /// </summary>
        internal static Dictionary<string, NamedIndexElement> NamedIndexElements
        {
            get
            {
                return _namedIndexElements;
            }
        }

        /// <summary>
        /// Gets named indexes Lucene directories
        /// </summary>
        internal static Dictionary<string, Directory> NamedIndexDirectories
        {
            get
            {
                return _namedIndexDirectories;
            }
        }

        /// <summary>
        /// Gets named reference indexes Lucene directories
        /// </summary>
        internal static Dictionary<string, Directory> ReferenceIndexDirectories
        {
            get
            {
                return _referenceIndexDirectories;
            }
        }

        /// <summary>
        /// Gets named indexes DirectoryInfos
        /// </summary>
        internal static Dictionary<string, System.IO.DirectoryInfo> MainDirectoryInfos
        {
            get
            {
                return _mainDirectoryInfos;
            }
        }

        /// <summary>
        /// Gets reference indexes DirectoryInfos
        /// </summary>
        internal static Dictionary<string, System.IO.DirectoryInfo> ReferenceDirectoryInfos
        {
            get
            {
                return _referenceDirectoryInfos;
            }
        }

        /// <summary>
        /// Gets Field properties for field
        /// </summary>
        internal static Dictionary<string, FieldProperties> FieldProperties
        {
            get
            {
                return _fieldProperties;
            }
        }

        /// <summary>
        /// Get fields that are made lowercase (case insensitive) in analysis
        /// </summary>
        internal static IList<string> LowercaseFields
        {
            get
            {
                return _lowercaseFields;
            }
        }

        /// <summary>
        /// Gets and sets the default index name
        /// </summary>
        internal static string DefaultIndexName
        {
            get
            {
                return _defaultIndexName;
            }
            set
            {
                _defaultIndexName = value;

                //re-set default directory
                if (NamedIndexDirectories[DefaultIndexName] != null)
                    DefaultDirectory = (Directory)NamedIndexDirectories[value];
            }
        }

        /// <summary>
        /// Gets client config elements
        /// </summary>
        internal static Dictionary<string, ClientElement> ClientElements
        {
            get
            {
                return _clientElements;
            }
        }

        #endregion

        #region Private

        private void LoadConfiguration()
        {
            MaxHitsForSearchResults = _indexingServiceOpts.MaxHitsForSearchResults;
            MaxHitsForReferenceSearch = _indexingServiceOpts.MaxHitsForReferenceSearch;
            MaxDisplayTextLength = _indexingServiceOpts.MaxDisplayTextLength;
            FIPSCompliant = _indexingServiceOpts.FIPSCompliant;

            foreach (ClientElement e in _indexingServiceOpts.Clients)
            {
                ClientElements.Add(e.Name, e);
            }

            foreach (NamedIndexElement e in _indexingServiceOpts.NamedIndexes.Indexes)
            {
                NamedIndexElements.Add(e.Name, e);
            }

            _defaultIndexName = _indexingServiceOpts.NamedIndexes.DefaultIndex;
        }

        private void LoadIndexes()
        {
            foreach (NamedIndexElement e in NamedIndexElements.Values)
            {
                System.IO.DirectoryInfo directoryMain = new System.IO.DirectoryInfo(System.IO.Path.Combine(e.GetDirectoryPath(), "Main"));
                System.IO.DirectoryInfo directoryRef = new System.IO.DirectoryInfo(System.IO.Path.Combine(e.GetDirectoryPath(), "Ref"));

                ReaderWriterLocks.Add(e.Name, new ReaderWriterLockSlim());
                ReaderWriterLocks.Add(e.Name + RefIndexSuffix, new ReaderWriterLockSlim());

                try
                {
                    if (!directoryMain.Exists)
                    {
                        directoryMain.Create();
                        Directory dir = _indexingServiceHandler.CreateIndex(e.Name, directoryMain);
                        NamedIndexDirectories.Add(e.Name, dir);
                    }
                    else
                    {
                        NamedIndexDirectories.Add(e.Name, FSDirectory.Open(directoryMain));
                    }

                    if (!directoryRef.Exists)
                    {
                        directoryRef.Create();
                        Directory refDir = _indexingServiceHandler.CreateIndex(e.Name + RefIndexSuffix, directoryRef);
                        ReferenceIndexDirectories.Add(e.Name, refDir);
                    }
                    else
                    {
                        ReferenceIndexDirectories.Add(e.Name, FSDirectory.Open(directoryRef));
                    }

                    MainDirectoryInfos.Add(e.Name, directoryMain);
                    ReferenceDirectoryInfos.Add(e.Name, directoryRef);

                    //IndexAnalyzers.Add(e.Name, new StandardAnalyzer(IndexingServiceSettings.LuceneVersion));    

                    //Set default index
                    DefaultDirectory = (Directory)NamedIndexDirectories[DefaultIndexName];
                    DefaultReferenceDirectory = (Directory)NamedIndexDirectories[DefaultIndexName];
                }
                catch (Exception ex)
                {
                    IndexingServiceServiceLog.Fatal(String.Format("Failed to load or create index: \"{0}\". Message: {1}", e.Name, ex.Message), ex);
                }
            }
        }

        private static void LoadFieldProperties()
        {
            _fieldProperties.Add(IdFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.NOT_ANALYZED });
            _fieldProperties.Add(TitleFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.ANALYZED });
            _fieldProperties.Add(DisplayTextFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.ANALYZED });
            _fieldProperties.Add(CreatedFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.NOT_ANALYZED });
            _fieldProperties.Add(ModifiedFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.NOT_ANALYZED });
            _fieldProperties.Add(PublicationEndFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.NOT_ANALYZED });
            _fieldProperties.Add(PublicationStartFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.NOT_ANALYZED });
            _fieldProperties.Add(UriFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.NO });
            _fieldProperties.Add(MetadataFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.NO });
            _fieldProperties.Add(CategoriesFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.ANALYZED });
            _fieldProperties.Add(CultureFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.NOT_ANALYZED });
            _fieldProperties.Add(AuthorsFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.ANALYZED });
            _fieldProperties.Add(TypeFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.ANALYZED });
            _fieldProperties.Add(ReferenceIdFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.NOT_ANALYZED });
            _fieldProperties.Add(AclFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.ANALYZED });
            _fieldProperties.Add(VirtualPathFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.ANALYZED });
            _fieldProperties.Add(AuthorStorageFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.NOT_ANALYZED });
            _fieldProperties.Add(NamedIndexFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.NOT_ANALYZED });
            _fieldProperties.Add(DefaultFieldName, new FieldProperties() { FieldStore = Field.Store.NO, FieldIndex = Field.Index.ANALYZED });
            _fieldProperties.Add(ItemStatusFieldName, new FieldProperties() { FieldStore = Field.Store.YES, FieldIndex = Field.Index.NOT_ANALYZED });
        }

        private static void LoadAnalyzer()
        {
            System.String[] stopWords = new System.String[] { };
            var fieldAnalyzers = new Dictionary<string, Analyzer>();

            // Untokenized fields uses keyword analyzer at all times
            fieldAnalyzers.Add(IndexingServiceSettings.IdFieldName, new KeywordAnalyzer());
            fieldAnalyzers.Add(IndexingServiceSettings.CultureFieldName, new KeywordAnalyzer());
            fieldAnalyzers.Add(IndexingServiceSettings.ReferenceIdFieldName, new KeywordAnalyzer());
            fieldAnalyzers.Add(IndexingServiceSettings.AuthorStorageFieldName, new KeywordAnalyzer());

            // Categories, ACL and VirtualPath field uses Whitespace analyzer at all times. Whitespace analyser leaves stop words and other non literal chars intact. 
            fieldAnalyzers.Add(IndexingServiceSettings.CategoriesFieldName, new WhitespaceAnalyzer(LuceneVersion));
            fieldAnalyzers.Add(IndexingServiceSettings.AclFieldName, new WhitespaceAnalyzer(LuceneVersion));
            fieldAnalyzers.Add(IndexingServiceSettings.VirtualPathFieldName, new WhitespaceAnalyzer(LuceneVersion));
            fieldAnalyzers.Add(IndexingServiceSettings.TypeFieldName, new WhitespaceAnalyzer(LuceneVersion));
            fieldAnalyzers.Add(IndexingServiceSettings.CreatedFieldName, new WhitespaceAnalyzer(LuceneVersion));
            fieldAnalyzers.Add(IndexingServiceSettings.ModifiedFieldName, new WhitespaceAnalyzer(LuceneVersion));
            fieldAnalyzers.Add(IndexingServiceSettings.PublicationEndFieldName, new WhitespaceAnalyzer(LuceneVersion));
            fieldAnalyzers.Add(IndexingServiceSettings.PublicationStartFieldName, new WhitespaceAnalyzer(LuceneVersion));
            fieldAnalyzers.Add(IndexingServiceSettings.ItemStatusFieldName, new WhitespaceAnalyzer(LuceneVersion));

            // Get the selected analyzer for the rest of the fields
            Analyzer indexAnalyzer = new StandardAnalyzer(LuceneVersion, StopFilter.MakeStopSet(LuceneVersion, stopWords));
            fieldAnalyzers.Add(IndexingServiceSettings.TitleFieldName, indexAnalyzer);
            fieldAnalyzers.Add(IndexingServiceSettings.DisplayTextFieldName, indexAnalyzer);
            fieldAnalyzers.Add(IndexingServiceSettings.AuthorsFieldName, indexAnalyzer);
            fieldAnalyzers.Add(IndexingServiceSettings.DefaultFieldName, indexAnalyzer);

            PerFieldAnalyzerWrapper perf = new PerFieldAnalyzerWrapper(new StandardAnalyzer(LuceneVersion, StopFilter.MakeStopSet(LuceneVersion, stopWords)), fieldAnalyzers);

            _analyzer = perf;
        }


        #endregion
    }
}
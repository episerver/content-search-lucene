using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.Threading;
using EPiServer.Logging.Compatibility;
using EPiServer.Search.IndexingService.Configuration;
using EPiServer.Search.IndexingService.Controllers;
using EPiServer.Search.IndexingService.Helpers;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EPiServer.Search.IndexingService
{
    public class IndexingServiceSettings : IIndexingServiceSettings
    {
        #region Member variables
        public const string TagsPrefix = "[[";
        public const string TagsSuffix = "]]";

        public const string DefaultFieldName = "EPISERVER_SEARCH_DEFAULT";
        public const string IdFieldName = "EPISERVER_SEARCH_ID";
        public const string TitleFieldName = "EPISERVER_SEARCH_TITLE";
        public const string DisplayTextFieldName = "EPISERVER_SEARCH_DISPLAYTEXT";
        public const string CreatedFieldName = "EPISERVER_SEARCH_CREATED";
        public const string ModifiedFieldName = "EPISERVER_SEARCH_MODIFIED";
        public const string PublicationEndFieldName = "EPISERVER_SEARCH_PUBLICATIONEND";
        public const string PublicationStartFieldName = "EPISERVER_SEARCH_PUBLICATIONSTART";
        public const string UriFieldName = "EPISERVER_SEARCH_URI";
        public const string CategoriesFieldName = "EPISERVER_SEARCH_CATEGORIES";
        public const string AuthorsFieldName = "EPISERVER_SEARCH_AUTHORS";
        public const string CultureFieldName = "EPISERVER_SEARCH_CULTURE";
        public const string TypeFieldName = "EPISERVER_SEARCH_TYPE";
        public const string ReferenceIdFieldName = "EPISERVER_SEARCH_REFERENCEID";
        public const string MetadataFieldName = "EPISERVER_SEARCH_METADATA";
        public const string AclFieldName = "EPISERVER_SEARCH_ACL";
        public const string VirtualPathFieldName = "EPISERVER_SEARCH_VIRTUALPATH";
        public const string DataUriFieldName = "EPISERVER_SEARCH_DATAURI";
        public const string AuthorStorageFieldName = "EPISERVER_SEARCH_AUTHORSTORAGE";
        public const string NamedIndexFieldName = "EPISERVER_SEARCH_NAMEDINDEX";
        public const string ItemStatusFieldName = "EPISERVER_SEARCH_ITEMSTATUS";

        public const string XmlQualifiedNamespace = "EPiServer.Search.IndexingService";
        public const string SyndicationItemAttributeNameCulture = "Culture";
        public const string SyndicationItemAttributeNameType = "Type";
        public const string SyndicationItemElementNameMetadata = "Metadata";
        public const string SyndicationItemAttributeNameNamedIndex = "NamedIndex";
        public const string SyndicationItemAttributeNameBoostFactor = "BoostFactor";
        public const string SyndicationItemAttributeNameIndexAction = "IndexAction";
        public const string SyndicationItemAttributeNameReferenceId = "ReferenceId";
        public const string SyndicationItemElementNameAcl = "ACL";
        public const string SyndicationItemAttributeNameDataUri = "DataUri";
        public const string SyndicationItemElementNameVirtualPath = "VirtualPath";
        public const string SyndicationItemAttributeNameScore = "Score";
        public const string SyndicationFeedAttributeNameVersion = "Version";
        public const string SyndicationFeedAttributeNameTotalHits = "TotalHits";
        public const string SyndicationItemAttributeNamePublicationEnd = "PublicationEnd";
        public const string SyndicationItemAttributeNamePublicationStart = "PublicationStart";
        public const string SyndicationItemAttributeNameItemStatus = "ItemStatus";
        public const string SyndicationItemAttributeNameAutoUpdateVirtualPath = "AutoUpdateVirtualPath";

        public const string RefIndexSuffix = "_ref";

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
        private readonly IHostEnvironment _hostEnvironment;
        private EpiserverFrameworkOptions _episerverFrameworkOpts;
        private readonly ILuceneHelper _luceneHelper;
        private readonly IDocumentHelper _documentHelper;
        #endregion

        #region Construct and Init

        public IndexingServiceSettings(IOptions<IndexingServiceOptions> indexingServiceOpts,
             IHostEnvironment hostEnvironment, 
             IOptions<EpiserverFrameworkOptions> episerverFrameworkOpts,
             ILuceneHelper luceneHelper,
             IDocumentHelper documentHelper)
        {
            _indexingServiceOpts = indexingServiceOpts.Value;
            _hostEnvironment = hostEnvironment;
            _episerverFrameworkOpts = episerverFrameworkOpts.Value;
            _luceneHelper = luceneHelper;
            _documentHelper = documentHelper;

            Init();
        }

        public void Dispose()
        {
        }

        public void Init()
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

        public static Lucene.Net.Util.LuceneVersion LuceneVersion
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
        public static int MaxHitsForSearchResults
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the maximum number of hits to be returned when doing a search in reference index. e.g. how many top comments should be included in the parent documents metadata
        /// </summary>
        public static int MaxHitsForReferenceSearch
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

        public static Analyzer Analyzer
        {
            get
            {
                if(_analyzer==null)
                    LoadAnalyzer();
                return _analyzer;
            }
        }

        /// <summary>
        /// Gets ReaderWriterLocks for named indexes
        /// </summary>
        public static Dictionary<string, ReaderWriterLockSlim> ReaderWriterLocks
        {
            get
            {
                return _readerWriterLocks;
            }
        }

        /// <summary>
        /// Gets named indexes config elements
        /// </summary>
        public static Dictionary<string, NamedIndexElement> NamedIndexElements
        {
            get
            {
                return _namedIndexElements;
            }
        }

        /// <summary>
        /// Gets named indexes Lucene directories
        /// </summary>
        public static Dictionary<string, Directory> NamedIndexDirectories
        {
            get
            {
                return _namedIndexDirectories;
            }
        }

        /// <summary>
        /// Gets named reference indexes Lucene directories
        /// </summary>
        public static Dictionary<string, Directory> ReferenceIndexDirectories
        {
            get
            {
                return _referenceIndexDirectories;
            }
        }

        /// <summary>
        /// Gets named indexes DirectoryInfos
        /// </summary>
        public static Dictionary<string, System.IO.DirectoryInfo> MainDirectoryInfos
        {
            get
            {
                return _mainDirectoryInfos;
            }
        }

        /// <summary>
        /// Gets reference indexes DirectoryInfos
        /// </summary>
        public static Dictionary<string, System.IO.DirectoryInfo> ReferenceDirectoryInfos
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
                if (_fieldProperties.Count == 0)
                    LoadFieldProperties();
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
                System.IO.DirectoryInfo directoryMain = new System.IO.DirectoryInfo(System.IO.Path.Combine(GetDirectoryPath(e.DirectoryPath), "Main"));
                System.IO.DirectoryInfo directoryRef = new System.IO.DirectoryInfo(System.IO.Path.Combine(GetDirectoryPath(e.DirectoryPath), "Ref"));

                ReaderWriterLocks.Add(e.Name, new ReaderWriterLockSlim());
                ReaderWriterLocks.Add(e.Name + RefIndexSuffix, new ReaderWriterLockSlim());

                try
                {
                    if (!directoryMain.Exists)
                    {
                        directoryMain.Create();
                        Directory dir = _documentHelper.CreateIndex(e.Name, directoryMain);
                        NamedIndexDirectories.Add(e.Name, dir);
                    }
                    else
                    {
                        NamedIndexDirectories.Add(e.Name, FSDirectory.Open(directoryMain));
                    }

                    if (!directoryRef.Exists)
                    {
                        directoryRef.Create();
                        Directory refDir = _documentHelper.CreateIndex(e.Name + RefIndexSuffix, directoryRef);
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
            _fieldProperties.Add(IdFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=NOT_ANALYZED
            _fieldProperties.Add(TitleFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=ANALYZED
            _fieldProperties.Add(DisplayTextFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=ANALYZED
            _fieldProperties.Add(CreatedFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=NOT_ANALYZED
            _fieldProperties.Add(ModifiedFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=NOT_ANALYZED
            _fieldProperties.Add(PublicationEndFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=NOT_ANALYZED
            _fieldProperties.Add(PublicationStartFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=NOT_ANALYZED
            _fieldProperties.Add(UriFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=NO
            _fieldProperties.Add(MetadataFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=NO
            _fieldProperties.Add(CategoriesFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=ANALYZED
            _fieldProperties.Add(CultureFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=NOT_ANALYZED
            _fieldProperties.Add(AuthorsFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=ANALYZED
            _fieldProperties.Add(TypeFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=ANALYZED
            _fieldProperties.Add(ReferenceIdFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=NOT_ANALYZED
            _fieldProperties.Add(AclFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=ANALYZED
            _fieldProperties.Add(VirtualPathFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=ANALYZED
            _fieldProperties.Add(AuthorStorageFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=NOT_ANALYZED
            _fieldProperties.Add(NamedIndexFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=NOT_ANALYZED
            _fieldProperties.Add(DefaultFieldName, new FieldProperties() { FieldStore = Field.Store.NO });//FieldIndex=ANALYZED
            _fieldProperties.Add(ItemStatusFieldName, new FieldProperties() { FieldStore = Field.Store.YES });//FieldIndex=NOT_ANALYZED
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

        public const String AppDataPathKey = "[appDataPath]";

        public String GetDirectoryPath(string directoryPath)
        {
            string path = directoryPath;

            if (path.StartsWith(AppDataPathKey, StringComparison.OrdinalIgnoreCase))
            {
                string basePath = _episerverFrameworkOpts.AppDataPath;
                if (String.IsNullOrEmpty(basePath))
                {
                    basePath = "App_Data";
                }
                path = System.IO.Path.Combine(basePath, path.Substring(AppDataPathKey.Length).TrimStart('\\', '/'));
            }
            path = Environment.ExpandEnvironmentVariables(path);
            if (!System.IO.Path.IsPathRooted(path))
            {
                path = System.IO.Path.Combine(_hostEnvironment.ContentRootPath ?? AppDomain.CurrentDomain.BaseDirectory, path);
            }
            return path;
        }

        #endregion
    }
}
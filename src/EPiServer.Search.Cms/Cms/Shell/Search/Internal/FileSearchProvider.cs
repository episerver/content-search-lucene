using System;
using System.Collections.Generic;
using System.Globalization;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Search;
using EPiServer.Search.Queries.Lucene;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Shell.Search;
using EPiServer.Web;
using EPiServer.Web.Routing;

namespace EPiServer.Cms.Shell.Search.Internal
{
    /// <summary>
    /// A search provider for searching for Files in EPiServer CMS
    /// </summary>
    [SearchProvider]
    public class FileSearchProvider : EPiServerSearchProviderBase<MediaData, ContentType>
    {
        /// <summary>
        /// Initialized a new instance of the <see cref="FileSearchProvider"/> class
        /// </summary>
        public FileSearchProvider(LocalizationService localizationService, ISiteDefinitionResolver siteDefinitionResolver, IContentTypeRepository contentTypeRepository, EditUrlResolver editUrlResolver, 
            ServiceAccessor<SiteDefinition> currentSiteDefinition, IContentRepository contentRepository, ILanguageBranchRepository languageBranchRepository, SearchHandler searchHandler, ContentSearchHandler contentSearchHandler, 
            SearchIndexConfig searchIndexConfig, UIDescriptorRegistry uiDescriptorRegistry, LanguageResolver languageResolver, UrlResolver urlResolver, TemplateResolver templateResolver)
            : base(localizationService, siteDefinitionResolver, contentTypeRepository, editUrlResolver, currentSiteDefinition, contentRepository, languageBranchRepository, searchHandler, contentSearchHandler, searchIndexConfig, uiDescriptorRegistry, languageResolver, urlResolver, templateResolver)
        {
        }

        /// <summary>
        /// Area that the provider maps to, used for spotlight searching
        /// </summary>
        /// <value>CMS</value>
        public override string Area { get { return ContentSearchProviderConstants.FileArea; } }

        /// <summary>
        /// Gets the Pages category
        /// </summary>
        /// <value>Pages</value>
        public override string Category { get { return LocalizationService.GetString(ContentSearchProviderConstants.FileCategory); } }

        /// <summary>
        /// Gets the localization path to Files.
        /// </summary>
        protected override string ToolTipResourceKeyBase
        {
            get
            {
                return ContentSearchProviderConstants.FileToolTipResourceKeyBase;
            }
        }

        /// <summary>
        /// Gets the name of the localization File type.
        /// </summary>
        protected override string ToolTipContentTypeNameResourceKey
        {
            get
            {
                return ContentSearchProviderConstants.FileToolTipContentTypeNameResourceKey;
            }
        }

        /// <summary>
        /// Gets the icon CSS class for Files.
        /// </summary>
        protected override string IconCssClass
        {
            get
            {
                return ContentSearchProviderConstants.FileIconCssClass;
            }
        }

        /// <summary>
        /// Adds the language filter.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="editAccessCultures">The edit access cultures.</param>
        /// <param name="cultureQuery">The culture query.</param>
        /// <remarks><see cref="FileSearchProvider"/> will not filter on language.</remarks>
        protected override void AddLanguageFilter(Query query, List<CultureInfo> editAccessCultures, GroupQuery cultureQuery)
        {
        }
    }
}

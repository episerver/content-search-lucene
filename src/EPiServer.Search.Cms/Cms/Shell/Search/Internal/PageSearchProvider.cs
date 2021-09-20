using System;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Search;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Shell.Search;
using EPiServer.Web;
using EPiServer.Web.Routing;

namespace EPiServer.Cms.Shell.Search.Internal
{
    /// <summary>
    /// A search provider for searing for pages in EPiServer CMS
    /// </summary>
    [SearchProvider]
    public class PageSearchProvider : EPiServerSearchProviderBase<PageData, PageType>
    {
        /// <summary>
        /// Initialized a new instance of the <see cref="PageSearchProvider"/> class
        /// </summary>
        public PageSearchProvider(LocalizationService localizationService, ISiteDefinitionResolver siteDefinitionResolver, IContentTypeRepository<PageType> pageTypeRepository, EditUrlResolver editUrlResolver, ServiceAccessor<SiteDefinition> currentSiteDefinition, IContentRepository contentRepository, ILanguageBranchRepository languageBranchRepository, SearchHandler searchHandler, ContentSearchHandler contentSearchHandler, SearchIndexConfig searchIndexConfig, UIDescriptorRegistry uiDescriptorRegistry, IContentLanguageAccessor languageResolver,
           UrlResolver urlResolver,
           TemplateResolver templateResolver)
           : base(localizationService, siteDefinitionResolver, pageTypeRepository, editUrlResolver, currentSiteDefinition, contentRepository, languageBranchRepository, searchHandler, contentSearchHandler, searchIndexConfig, uiDescriptorRegistry, languageResolver, urlResolver, templateResolver)
        {
        }

        /// <summary>
        /// Area that the provider maps to, used for spotlight searching
        /// </summary>
        /// <value>CMS pages</value>
        public override string Area { get { return ContentSearchProviderConstants.PageArea; } }

        /// <summary>
        /// Gets the Pages category
        /// </summary>
        /// <value>Pages</value>
        public override string Category { get { return LocalizationService.GetString(ContentSearchProviderConstants.PageCategory); } }

        /// <summary>
        /// Gets the page localization path.
        /// </summary>
        protected override string ToolTipResourceKeyBase
        {
            get
            {
                return ContentSearchProviderConstants.PageToolTipResourceKeyBase;
            }
        }

        /// <summary>
        /// Gets the name of the localization page type.
        /// </summary>
        protected override string ToolTipContentTypeNameResourceKey
        {
            get
            {
                return ContentSearchProviderConstants.PageToolTipContentTypeNameResourceKey;
            }
        }

        /// <summary>
        /// Gets the icon CSS class for pages.
        /// </summary>
        protected override string IconCssClass
        {
            get
            {
                return ContentSearchProviderConstants.PageIconCssClass;
            }
        }
    }
}

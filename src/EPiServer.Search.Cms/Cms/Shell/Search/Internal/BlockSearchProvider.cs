using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Search;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Shell.Search;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Http;

namespace EPiServer.Cms.Shell.Search.Internal
{
    /// <summary>
    /// A search provider for searing for blocks in EPiServer CMS
    /// </summary>
    [SearchProvider]
    public class BlockSearchProvider : EPiServerSearchProviderBase<BlockData, BlockType>
    { 
        /// <summary>
        /// Initialized a new instance of the <see cref="BlockSearchProvider"/> class
        /// </summary>
        public BlockSearchProvider(LocalizationService localizationService, ISiteDefinitionResolver siteDefinitionResolver, IContentTypeRepository<BlockType> blockTypeRepository, EditUrlResolver editUrlResolver, 
            ServiceAccessor<SiteDefinition> currentSiteDefinition, IContentRepository contentRepository, ILanguageBranchRepository languageBranchRepository, SearchHandler searchHandler, ContentSearchHandler contentSearchHandler, 
            SearchIndexConfig searchIndexConfig, UIDescriptorRegistry uiDescriptorRegistry, IContentLanguageAccessor languageResolver, IUrlResolver urlResolver, ITemplateResolver templateResolver, IBlobResolver blobResolver, 
            HttpContext httpContext, IPrincipalAccessor principalContext)
           : base(localizationService, siteDefinitionResolver, blockTypeRepository, editUrlResolver, currentSiteDefinition, contentRepository, languageBranchRepository, searchHandler, contentSearchHandler, searchIndexConfig, uiDescriptorRegistry, languageResolver, urlResolver, templateResolver, httpContext, principalContext)
        {
            BlobResolver = new Injected<IBlobResolver>(blobResolver);
        }

        /// <summary>
        /// Area that the provider maps to, used for spotlight searching
        /// </summary>
        /// <value>CMS</value>
        public override string Area { get { return ContentSearchProviderConstants.BlockArea; } }

        /// <summary>
        /// Gets the Pages category
        /// </summary>
        /// <value>Pages</value>
        public override string Category { get { return LocalizationService.GetString(ContentSearchProviderConstants.BlockCategory); } }

        /// <summary>
        /// Gets the localization path to blocks.
        /// </summary>
        protected override string ToolTipResourceKeyBase
        {
            get
            {
                return ContentSearchProviderConstants.BlockToolTipResourceKeyBase;
            }
        }

        /// <summary>
        /// Gets the name of the localization block type.
        /// </summary>
        protected override string ToolTipContentTypeNameResourceKey
        {
            get
            {
                return ContentSearchProviderConstants.BlockToolTipContentTypeNameResourceKey;
            }
        }

        /// <summary>
        /// Gets the icon CSS class for blocks.
        /// </summary>
        protected override string IconCssClass
        {
            get
            {
                return ContentSearchProviderConstants.BlockIconCssClass;
            }
        }

        /// <summary>
        /// Remove duplicate hits and try to select the most appropriate language version
        /// </summary>
        /// <param name="query">The query</param>
        /// <param name="searchResults">The search result from the provider.</param>
        protected override IEnumerable<BlockData> FilterResults(Query query, IEnumerable<BlockData> searchResults)
        {
            // If the search is a global search then don't filter on preferred culture and just return.
            if (query.Parameters != null && query.Parameters.ContainsKey("global") && Boolean.Parse(query.Parameters["global"].ToString()))
            {
                return searchResults;
            }

            var filteredResults = new List<IContent>();

            // Cast all blocks to IContent so we can get the contentLink
            var contentResults = searchResults.Cast<IContent>().ToList();
            foreach (var content in contentResults)
            {
                //If it allready have been added do not add it again
                if (filteredResults.Any(c => c.ContentLink == content.ContentLink))
                {
                    continue;
                }

                //Check for duplicates
                var duplicates = contentResults.Where(c => c.ContentLink.Equals(content.ContentLink)).ToList();
                var hasDuplicates = duplicates.Count() > 1;

                // If there are no duplicates
                if (!hasDuplicates)
                {
                    // If the content is in the preferred culture on is the master language branch return that
                    // otherwise let the system get the most appropriate version
                    if (content.IsInCulture(LanguageResolver.Language) || content.IsMasterLanguageBranch())
                    {
                        filteredResults.Add(content);
                    }
                }
                else
                {
                    //If one of the duplicates match the preferred culture return that
                    var matchingPreferredCulture = duplicates.FirstOrDefault(d => d.IsInCulture(LanguageResolver.Language));
                    if (matchingPreferredCulture != null)
                    {
                        filteredResults.Add(matchingPreferredCulture);
                        continue;
                    }

                    //If one of the duplicates is in the master language return that
                    var matchingMasterLanguageBranch = duplicates.FirstOrDefault(d => d.IsMasterLanguageBranch());
                    if (matchingMasterLanguageBranch != null)
                    {
                        filteredResults.Add(matchingMasterLanguageBranch);
                        continue;
                    }

                    filteredResults.Add(duplicates.First());
                }
            }

            //Return a list of blocks
            return filteredResults.Cast<BlockData>();
        }
    }
}

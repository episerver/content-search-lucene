namespace EPiServer.Cms.Shell.Search.Internal
{
    /// <summary>
    /// Constants used in content search providers.
    /// </summary>
    public static class ContentSearchProviderConstants
    {
        #region Page

        /// <summary>
        /// Area that the provider maps to, used for spotlight searching
        /// </summary>
        /// <value>CMS pages</value>
        public static string PageArea { get { return "CMS/pages"; } }

        /// <summary>
        /// Gets the Pages category
        /// </summary>
        /// <value>Pages</value>
        public static string PageCategory { get { return "/shell/cms/search/pages/category"; } }

        /// <summary>
        /// Gets the page localization path.
        /// </summary>
        public static string PageToolTipResourceKeyBase { get { return "/shell/cms/search/pages/tooltip"; } }

        /// <summary>
        /// Gets the name of the localization page type.
        /// </summary>
        public static string PageToolTipContentTypeNameResourceKey { get { return "pagetype"; } }

        /// <summary>
        /// Gets the icon CSS class for pages.
        /// </summary>
        public static string PageIconCssClass { get { return "epi-resourceIcon epi-resourceIcon-page"; } }

        #endregion

        #region Block

        /// <summary>
        /// Area that the provider maps to, used for spotlight searching
        /// </summary>
        /// <value>CMS</value>
        public static string BlockArea { get { return "CMS/blocks"; } }

        /// <summary>
        /// Gets the Pages category
        /// </summary>
        /// <value>Pages</value>
        public static string BlockCategory { get { return "/shell/cms/search/blocks/category"; } }

        /// <summary>
        /// Gets the localization path to blocks.
        /// </summary>
        public static string BlockToolTipResourceKeyBase { get { return "/shell/cms/search/blocks/tooltip"; } }

        /// <summary>
        /// Gets the name of the localization block type.
        /// </summary>
        public static string BlockToolTipContentTypeNameResourceKey { get { return "blocktype"; } }

        /// <summary>
        /// Gets the icon CSS class for blocks.
        /// </summary>
        public static string BlockIconCssClass { get { return "epi-resourceIcon epi-resourceIcon-block"; } }

        #endregion

        #region Files

        /// <summary>
        /// Area that the provider maps to, used for spotlight searching
        /// </summary>
        /// <value>CMS</value>
        public static string FileArea { get { return "CMS/files"; } }

        /// <summary>
        /// Gets the Pages category
        /// </summary>
        /// <value>Pages</value>
        public static string FileCategory { get { return "/shell/cms/search/files/category"; } }

        /// <summary>
        /// Gets the localization path to blocks.
        /// </summary>
        public static string FileToolTipResourceKeyBase { get { return "/shell/cms/search/files/tooltip"; } }

        /// <summary>
        /// Gets the name of the localization block type.
        /// </summary>
        public static string FileToolTipContentTypeNameResourceKey { get { return "filetype"; } }

        /// <summary>
        /// Gets the icon CSS class for blocks.
        /// </summary>
        public static string FileIconCssClass { get { return "epi-resourceIcon epi-resourceIcon-file"; } }

        #endregion
    }
}
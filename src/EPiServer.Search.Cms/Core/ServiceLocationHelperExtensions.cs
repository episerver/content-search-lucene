using EPiServer.ServiceLocation;

namespace EPiServer.Core
{
    /// <summary>  
    /// Makes EPiServer.Core services part of the public API.  
    /// </summary>  
    public static class SearchServiceLocationHelperExtensions
    {
        /// <summary>  
        /// Resolves the <see cref="ContentSearchHandler"/> service.
        /// </summary>  
        /// <param name="serviceLocationHelper">The service location helper.</param>  
        /// <returns>An instance of the currently registered <see cref="ContentSearchHandler"/> service.</returns>  
        public static ContentSearchHandler ContentSearchHandler(this ServiceProviderHelper serviceLocationHelper) => serviceLocationHelper.Advanced.GetInstance<ContentSearchHandler>();
    }
}

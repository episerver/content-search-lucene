using log4net;

namespace EPiServer.Search.IndexingService
{
    public interface IIndexingServiceSettings
    {
        void Init();
        static ILog IndexingServiceServiceLog { get; set; }
    }
}

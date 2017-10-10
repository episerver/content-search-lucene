using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Logging;

namespace EPiServer.Search.Internal
{
    public class ReIndexManager : IReIndexManager
    {
        private static readonly ILogger _log = LogManager.GetLogger();
        private readonly SearchHandler _searchHandler;
        private readonly IEnumerable<IReIndexable> _reindexables;

        public ReIndexManager(SearchHandler searchHandler, IEnumerable<IReIndexable> reindexables)
        {
            _searchHandler = searchHandler;
            _reindexables = reindexables ?? Enumerable.Empty<IReIndexable>();
        }

        public void ReIndex()
        {
            foreach (var reindexable in _reindexables)
            {
                try
                {
                    _log.Information($"Start Reset index of the type: {reindexable.GetType()}, Startup Time: {DateTime.Now}");

                    _searchHandler.ResetIndex(reindexable.NamedIndexingService, reindexable.NamedIndex);

                    _log.Information($"Finish Reset index of the type: {reindexable.GetType()}, Finished Time: {DateTime.Now}");
                }
                catch (Exception e)
                {
                    _log.Error($"Failed to reset index of the service type: {reindexable.GetType()}", e);
                }
            }


            foreach (var reindexable in _reindexables)
            {
                try
                {
                    _log.Information($"Start re-indexing of the type: {reindexable.GetType()}, Startup Time: {DateTime.Now}.");

                    reindexable.ReIndex();

                    _log.Information($"Finish re-indexing of the type: {reindexable.GetType()}, Finished Time: {DateTime.Now}.");
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to re-index the type: {reindexable.GetType()}", ex);
                }
            }
        }
    }
}

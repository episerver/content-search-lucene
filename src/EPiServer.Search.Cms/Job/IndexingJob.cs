using System;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.PlugIn;
using EPiServer.ServiceLocation;

namespace EPiServer.Job
{
    /// <summary>
    /// Scheduled job that indexes CMS contents.
    /// </summary>
    [ScheduledPlugIn(
        DefaultEnabled = true,
        LanguagePath = "/EPiServer/Search.Cms/indexingJob",
        HelpFile = "OptimizelySearchNavigationrelatedscheduledjobs",
        SortIndex = 0,
        InitialTime = "2:00:0", IntervalLength = 1, IntervalType = ScheduledIntervalType.Days, Restartable = true)]
    [ServiceConfiguration]
    internal class IndexingJob : Scheduler.ScheduledJobBase
    {
        private readonly IIndexingJobService _indexingJobService;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexingJob"/> class.
        /// </summary>
        /// <param name="indexingJobService">The indexing job service.</param>
        public IndexingJob(IIndexingJobService indexingJobService)
        {
            IsStoppable = true;
            _indexingJobService = indexingJobService;
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        public override string Execute()
        {
            using var contentCacheScope = new ContentCacheScope { SlidingExpiration = TimeSpan.Zero };
            return _indexingJobService.Start(OnStatusChanged);
        }

        /// <summary>
        /// Stops the indexing job.
        /// </summary>
        public override void Stop() => _indexingJobService.Stop();

        /// <summary>
        /// Verifies the indexing job is stopped or not.
        /// </summary>
        public bool IsStopped() => _indexingJobService.IsStopped();
    }
}

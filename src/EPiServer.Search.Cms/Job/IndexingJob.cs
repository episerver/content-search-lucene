using EPiServer.PlugIn;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPiServer.Job
{
    /// <summary>
    /// Scheduled job that indexes CMS contents.
    /// </summary>
    [ScheduledPlugIn(
        DisplayName = "EPiServer Basic Search Content Indexing Job",
        DefaultEnabled = true,
        Description = "This indexing job is used to reindex all content. During normal operation changes to content are being indexed as they are made without rerunning or scheduling of this job.",
        SortIndex = 0)]
    [ServiceConfiguration]
    class IndexingJob : Scheduler.ScheduledJobBase
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
            return _indexingJobService.Start(OnStatusChanged);
        }

        /// <summary>
        /// Stops the indexing job.
        /// </summary>
        public override void Stop()
        {
            _indexingJobService.Stop();
        }

        /// <summary>
        /// Verifies the indexing job is stopped or not.
        /// </summary>
        public bool IsStopped()
        {
            return _indexingJobService.IsStopped();
        }
    }
}

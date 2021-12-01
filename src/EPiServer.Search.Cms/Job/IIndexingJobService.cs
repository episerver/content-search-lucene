using System;

namespace EPiServer.Job
{
    /// <summary>
    /// The interface defines indexing job service in EPiServer.Find.Cms
    /// </summary>
    public interface IIndexingJobService
    {
        /// <summary>
        /// Starts indexing job.
        /// </summary>
        /// <returns>The job report.</returns>
        string Start();

        /// <summary>
        /// Starts indexing job.
        /// </summary>
        /// <param name="statusNotification">The notification action when job status changed.</param>
        /// <returns>The job report.</returns>
        string Start(Action<string> statusNotification);

        /// <summary>
        /// Stops the job.
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets stop status of the job.
        /// </summary>
        bool IsStopped();
    }
}

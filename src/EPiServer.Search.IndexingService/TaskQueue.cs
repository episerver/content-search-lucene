using System.Threading;
using System.Collections;
using System;

namespace EPiServer.Search.IndexingService
{
    /// <summary>
    /// Class that allows for enqueueing of delegates to be invoked.
    /// </summary>
    public class TaskQueue
    {
        private readonly System.Timers.Timer _queueFlushTimer;
        private readonly Queue _queue = Queue.Synchronized(new Queue());
        private readonly double _timerInterval;
        private readonly TimeSpan _minQueueItemAge;
        private readonly string _queueName;

        /// <summary>
        /// Constructs a TaskQueue
        /// </summary>
        /// <param name="queueName">Queue identifier used for logging purposes</param>
        /// <param name="timerInterval">Interval in milliseconds telling when the queue should be processed</param>
        /// <param name="minQueueItemAge">The minimum age of a queue item in order for it to be dequeued</param>
        public TaskQueue(string queueName, double timerInterval, TimeSpan minQueueItemAge)
        {
            _queueName = queueName;
            _timerInterval = timerInterval;
            _minQueueItemAge = minQueueItemAge;
            _queueFlushTimer = new System.Timers.Timer(_timerInterval);
            _queueFlushTimer.AutoReset = false;
            _queueFlushTimer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
        }

        /// <summary>
        /// Adds an <see cref="Action"/> to the queue
        /// </summary>
        /// <param name="task">The <see cref="Action"/> to add to the queue</param>
        public void Enqueue(Action task)
        {
            _queue.Enqueue(new QueueItem(task));
            _queueFlushTimer.Enabled = true;
        }


        #region Timer

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                while (_queue.Count > 0 && ((QueueItem)_queue.Peek()).Time < DateTime.Now.Add(-_minQueueItemAge))
                {
                    try
                    {
                        ((QueueItem)_queue.Dequeue()).Task();
                    }
                    catch (Exception ex)
                    {
                        IndexingServiceSettings.IndexingServiceServiceLog.Error(
                            string.Format("An exception was thrown when task was invoked by TaskQueue: '{0}'. The message was: {1}. Stacktrace was: {2}", _queueName, ex.Message, ex.StackTrace));
                    }
                }
                OnQueueProcessed();
            }
            catch (Exception ex)
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Error(
                        string.Format("An exception was thrown while processing TaskQueue: '{0}'. The message was: {1}. Stacktrace was: {2}", _queueName, ex.Message, ex.StackTrace));
            }
            finally
            {
                // Ensure that the timer is started again if the queue was not emptied.
                if (_queue.Count > 0)
                {
                    _queueFlushTimer.Enabled = true;
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets the current length of the queue
        /// </summary>
        public int QueueLength
        {
            get
            {
                return _queue.Count;
            }
        }

        #region Events

        /// <summary>
        /// Occurs when the queue has been processed
        /// </summary>
        public event EventHandler QueueProcessed;

        private void OnQueueProcessed()
        {
            if (QueueProcessed != null)
            {
                QueueProcessed(null, new EventArgs());
            }
        }

        #endregion

        internal class QueueItem
        {
            internal QueueItem(Action task)
            {
                Task = task;
                Time = DateTime.Now;
            }

            internal Action Task
            {
                get;
                set;
            }

            internal DateTime Time
            {
                get;
                set;
            }
        }
    }
}
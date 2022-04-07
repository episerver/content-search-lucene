using System;
using System.Text;
using EPiServer.Data.Dynamic;

namespace EPiServer.Search.Data
{
    [EPiServerDataStore(AutomaticallyRemapStore = true)]
    public class ContentIndexingResult : IDynamicData
    {
        #region IDynamicData Members

        public EPiServer.Data.Identity Id
        {
            get;
            set;
        }

        #endregion

        public string Message { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int IndexingCount { get; set; } = 0;

        public int NumberOfContentErrors { get; set; } = 0;

        public bool HasErrors
        {
            get { return NumberOfContentErrors > 0; }
        }

        public string PrintReport()
        {
            var report = new StringBuilder();
            var duration = EndTime.Subtract(StartTime);
            report.AppendLine("Reindexing completed.");
            if (!String.IsNullOrEmpty(Message))
            {
                report.AppendLine($"{Message}.");
            }
            report.AppendLine($"ExecutionTime: {Math.Floor(duration.TotalHours)} hours {duration.Minutes} minutes {duration.Seconds} seconds.");
            report.AppendLine($"Number of contents indexed: {IndexingCount}.");

            if (HasErrors)
            {
                report.AppendLine($"Number of content errors: {NumberOfContentErrors}.");
            }

            return report.ToString();
        }

    }
}

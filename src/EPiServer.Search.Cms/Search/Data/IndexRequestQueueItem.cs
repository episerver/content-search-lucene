using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EPiServer.Data.Dynamic;

namespace EPiServer.Search.Data
{
    /// <internal-api />
    [EPiServerDataTable(TableName = "tblIndexRequestLog")]
    [EPiServerDataStore(AutomaticallyRemapStore = true)]
    public class IndexRequestQueueItem : IDynamicData
    {
        #region IDynamicData Members

        public EPiServer.Data.Identity Id
        {
            get;
            set;
        }

        #endregion

        public string IndexItemId
        {
            get;
            set;
        }

        [EPiServerDataIndex]
        public string NamedIndexingService
        {
            get;
            set;
        }

        public string FeedItemJson
        {
            get;
            set;
        }

        [EPiServerDataIndex]
        public DateTime Timestamp
        {
            get;
            set;
        }

        [EPiServerDataIndex]
        public String NamedIndex
        {
            get;
            set;
        }

    }
}

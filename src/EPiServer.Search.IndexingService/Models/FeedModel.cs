using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService
{
    public class FeedModel
    {
        public string Id { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
        public IEnumerable<FeedItemModel> Items { get; set; }
        public Dictionary<string, string> AttributeExtensions { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, object> ElementExtensions { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }
    public class FeedItemModel
    {
        public string Id { get; set; }
        public Uri Uri { get; set; }
        public string Title { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }
        public string Culture { get; set; }
        public string ItemType { get; set; }
        public string DisplayText { get; set; }

        public Dictionary<string,string> AttributeExtensions { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, object> ElementExtensions { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public Collection<string> Categories { get; } = new Collection<string>();
        public Collection<string> Authors { get; } = new Collection<string>();        
    }
}
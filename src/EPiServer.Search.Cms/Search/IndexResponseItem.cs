namespace EPiServer.Search
{
    public class IndexResponseItem : IndexItemBase
    {
        public IndexResponseItem(string id)
            : base(id) { }

        public float Score { get; set; }
    }
}

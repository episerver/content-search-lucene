using System.ServiceModel;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;

namespace EPiServer.Search.IndexingService
{
    [ServiceContract(Namespace = "EPiServer.Search.IndexingService")]
    [ServiceKnownType(typeof(Atom10FeedFormatter))]
    [ServiceKnownType(typeof(SyndicationFeedFormatter))]
    public interface IIndexingService
    {
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "reset/?namedindex={namedindex}&accessKey={accessKey}", ResponseFormat = WebMessageFormat.Xml,
            RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        void ResetIndex(string namedIndex, string accessKey);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "update/?accessKey={accessKey}", ResponseFormat = WebMessageFormat.Xml,
            RequestFormat = WebMessageFormat.Xml, BodyStyle = WebMessageBodyStyle.Bare)]
        void UpdateIndex(string accessKey, SyndicationFeedFormatter formatter);

        [OperationContract]
        [WebGet(UriTemplate = "search/?q={q}&namedindexes={namedindexes}&format=xml&offset={offset}&limit={limit}&accesskey={accesskey}", 
            ResponseFormat=WebMessageFormat.Xml, 
            BodyStyle=WebMessageBodyStyle.Bare)]
        SyndicationFeedFormatter GetSearchResultsXml(string q, string namedIndexes, string offset, string limit, string accessKey);

        [OperationContract]
        [WebGet(UriTemplate = "search/?q={q}&namedindexes={namedindexes}&format=json&offset={offset}&limit={limit}&accesskey={accesskey}", 
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        SyndicationFeedFormatter GetSearchResultsJson(string q, string namedIndexes, string offset, string limit, string accessKey);

        [OperationContract]
        [WebGet(UriTemplate = "namedindexes/?accesskey={accesskey}",
            ResponseFormat = WebMessageFormat.Xml,
            BodyStyle = WebMessageBodyStyle.Bare)]
        SyndicationFeedFormatter GetNamedIndexes(string accessKey);
    }
}

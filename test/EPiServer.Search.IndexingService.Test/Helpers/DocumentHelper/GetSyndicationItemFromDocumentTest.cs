using Lucene.Net.Documents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.DocumentHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper), nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper.GetSyndicationItemFromDocument))]
    public class GetSyndicationItemFromDocumentTest : DocumentHelperTestBase
    {
        [Fact]
        public void GetSyndicationItemFromDocument_ShouldReturnNotNull()
        {
            var doc = new Document();
            var itemId = Guid.NewGuid().ToString();

            doc.Add(new TextField(IndexingServiceSettings.NamedIndexFieldName, "testindex1", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.IdFieldName, itemId, Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.TitleFieldName, "Header", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.DisplayTextFieldName, "Body", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.ModifiedFieldName, Regex.Replace(DateTime.Now.ToString("u", CultureInfo.InvariantCulture), @"\D", ""), Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.CreatedFieldName, Regex.Replace(DateTime.Now.AddDays(-1).ToString("u", CultureInfo.InvariantCulture), @"\D", ""), Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.UriFieldName, "http://www.google.com", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.CultureFieldName, "sv-SE", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.ItemStatusFieldName, "1", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.TypeFieldName, "EPiServer.Search.IndexItem, EPiServer.Search", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.SyndicationItemAttributeNameDataUri, "http://www.google.com", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.SyndicationItemAttributeNameBoostFactor, "1", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.PublicationEndFieldName, Regex.Replace(DateTime.Now.AddDays(1).ToString("u", CultureInfo.InvariantCulture), @"\D", ""), Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.PublicationStartFieldName, Regex.Replace(DateTime.Now.AddDays(-1).ToString("u", CultureInfo.InvariantCulture), @"\D", ""), Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.MetadataFieldName, "Metadata", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.CategoriesFieldName, "[[cat1]] [[cat2]]", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.AuthorsFieldName, "author1|author2", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.AclFieldName, "[[group1]] [[group2]]", Field.Store.YES));
            doc.Add(new TextField(IndexingServiceSettings.VirtualPathFieldName, "vp1|vp2", Field.Store.YES));

            ((TextField)doc.Fields[1]).Boost = 1;

            var scoreDoc = new ScoreDocument(doc, 1);

            var classInstant = SetupMock();
            var result = classInstant.GetSyndicationItemFromDocument(scoreDoc);
            Assert.NotNull(result);
        }
    }
}

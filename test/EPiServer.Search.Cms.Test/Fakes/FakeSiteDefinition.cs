using System;
using EPiServer.Core;
using EPiServer.Web;

namespace EPiServer.Cms.Shell.UI.Test.Fakes
{
    public class FakeSiteDefinition : SiteDefinition
    {
        private ContentReference _rootPage;
        private readonly ContentReference _contentAssetsRoot;
        private readonly ContentReference _globalAssetsRoot;
        private readonly ContentReference _wasteBasket;

        public FakeSiteDefinition()
            : this(
            new ContentReference(1),
            new ContentReference(2),
            new ContentReference(3),
            new ContentReference(4),
            new ContentReference(5),
            new ContentReference(6))
        {
        }

        public FakeSiteDefinition(
            ContentReference rootPage,
            ContentReference contentAssetsRoot,
            ContentReference globalAssetsRoot,
            ContentReference wasteBasket,
            ContentReference startPage,
            ContentReference siteAssetsRoot)
        {
            _rootPage = rootPage;
            _contentAssetsRoot = contentAssetsRoot;
            _globalAssetsRoot = globalAssetsRoot;
            _wasteBasket = wasteBasket;

            StartPage = startPage;
            SiteAssetsRoot = siteAssetsRoot;

            SiteUrl = new Uri("http://fakesite.com");
        }

        public override ContentReference RootPage => _rootPage;

        public void SetRootPage(ContentReference reference) => _rootPage = reference;

        public override ContentReference ContentAssetsRoot => _contentAssetsRoot;

        public override ContentReference GlobalAssetsRoot => _globalAssetsRoot;

        public override ContentReference WasteBasket => _wasteBasket;
    }
}

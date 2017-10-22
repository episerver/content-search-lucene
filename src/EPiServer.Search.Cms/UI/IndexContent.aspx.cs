using System;
using System.Linq;
using System.Threading;
using EPiServer.Core;
using EPiServer.Data.Dynamic;
using EPiServer.Logging;
using EPiServer.Search;
using EPiServer.Shell.WebForms;
using EPiServer.ServiceLocation;
using EPiServer.PlugIn;
using System.Security.Permissions;

namespace EPiServer.UI.Admin
{
    /// <summary>
    /// Admin tool used to re-index the content of the site.
    /// </summary>
    [GuiPlugIn(DisplayName = "Reindex Content", Area = PlugInArea.AdminMenu, UrlFromModuleFolder = "IndexContent.aspx", LanguagePath = "/admin/indexcontent", SortIndex = 900)]
    [PrincipalPermission(SecurityAction.Demand, Role = "WebAdmins")]
    [PrincipalPermission(SecurityAction.Demand, Role = "Administrators")]
    public partial class IndexContent : WebFormsBase
    {
        private static readonly ILogger _log = LogManager.GetLogger();

        private DynamicDataStore Store
        {
            get
            {
                return DynamicDataStoreFactory.Instance.GetStore(typeof(IndexingInformation)) ??
                    DynamicDataStoreFactory.Instance.CreateStore(typeof(IndexingInformation));
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            SystemMessageContainer.Heading = Translate("/admin/indexcontent/heading");
            SystemMessageContainer.Description = Translate("/admin/indexcontent/description");
            base.OnLoad(e);
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            IndexingInformation indexingInformation = Store.LoadAll<IndexingInformation>().FirstOrDefault(elem => elem != null);

            if (indexingInformation != null)
            {
                IndexDate.Text = indexingInformation.ExecutionDate.ToString("g");
            }
        }

        protected void IndexContent_Click(object sender, EventArgs e)
        {
            if (!ServiceLocator.Current.GetInstance<SearchOptions>().Active)
            {
                return;
            }
            
            ThreadPool.QueueUserWorkItem(new WaitCallback(ReIndex));
        }

        private void ReIndex(object state)
        {
            try
            {
                if (ResetIndex.Checked)
                {
                    ServiceLocator.Current.GetInstance<IReIndexManager>().ReIndex();
                }

                SaveIndexingInformation();
            }
            catch (Exception ex)
            {
                // We do not want any type of exception left unhandled since we are running on a separete thread.
                _log.Error("Error when indexing content. ", ex);
            }
        }

        private void SaveIndexingInformation()
        {
            IndexingInformation indexingInformation = Store.LoadAll<IndexingInformation>().FirstOrDefault(elem => elem != null);

            if (indexingInformation == null)
            {
                indexingInformation = new IndexingInformation();
            }

            indexingInformation.ExecutionDate = DateTime.Now;
            indexingInformation.ResetIndex = ResetIndex.Checked;

            Store.Save(indexingInformation);
        }
    }
}
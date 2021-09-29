using EPiServer.Shell.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPiServer.Search.Cms.Search
{
    public class SearchModule : ShellModule
    {
        public SearchModule(string name, string routeBasePath, string resourceBasePath)
            : base(name, routeBasePath, resourceBasePath)
        {

        }
    }
}
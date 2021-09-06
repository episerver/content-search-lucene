using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using Microsoft.Extensions.Hosting;

namespace EPiServer.Search.IndexingService.Configuration
{
    [ConfigurationCollection(typeof(NamedIndexElement))]
    public class NamedIndexCollection : ConfigurationElementCollection
    {
        private readonly IHostEnvironment _hostEnvironment;
        public NamedIndexCollection(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }
        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        public NamedIndexElement this[int index]
        {
            get { return (NamedIndexElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);
                BaseAdd(index, value);
            }
        }

        public void Add(ConfigurationElement element)
        {
            BaseAdd(element);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new NamedIndexElement(_hostEnvironment);
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((NamedIndexElement)element).Name;
        }

        public void Remove(NamedIndexElement element)
        {
            BaseRemove(element.Name);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }
    }
}

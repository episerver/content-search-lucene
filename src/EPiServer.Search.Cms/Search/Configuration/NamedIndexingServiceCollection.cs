 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace EPiServer.Search.Configuration
{
    [ConfigurationCollection(typeof(NamedIndexingServiceElement))]
    public class NamedIndexingServiceCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        public NamedIndexingServiceElement this[int index]
        {
            get { return (NamedIndexingServiceElement)BaseGet(index); }
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
            return new NamedIndexingServiceElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((NamedIndexingServiceElement)element).Name;
        }

        public void Remove(NamedIndexingServiceElement element)
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
 
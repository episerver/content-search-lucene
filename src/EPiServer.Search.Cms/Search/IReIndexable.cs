using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EPiServer.Search
{
    /// <summary>
    /// Defines an interface for performing Reindexing of items
    /// </summary>
    public interface IReIndexable
    {

        /// <summary>
        /// Re-Index.
        /// </summary>
        void ReIndex();

        /// <summary>
        /// Gets the NamedIndex.
        /// </summary>
        String NamedIndex { get; }

        /// <summary>
        /// Gets the named indexing service.
        /// </summary>
        String NamedIndexingService { get; }
    }
}

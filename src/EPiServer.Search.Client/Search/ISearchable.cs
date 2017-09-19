using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EPiServer.Search
{
    /// <summary>
    /// Describes the searchability of an item.
    /// </summary>
    public interface ISearchable
    {
        /// <summary>
        /// Gets a value indicating whether this instance is searchable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is searchable; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// The value of this property will be used by the search handlers to decide if 
        /// this item should be added to the search index.
        /// </remarks>
        bool IsSearchable { get; }

        /// <summary>
        /// Gets a value indicating whether children of this content should be indexed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if children should be indexed; otherwise, <c>false</c>.
        /// </value>
        bool AllowReIndexChildren { get; }
    }
}

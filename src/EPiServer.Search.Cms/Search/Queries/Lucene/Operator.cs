using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EPiServer.Search.Queries.Lucene
{
    public enum LuceneOperator
    {
        None = 0,
        AND = 1,
        OR = 2,
        NOT = 3
    }
}

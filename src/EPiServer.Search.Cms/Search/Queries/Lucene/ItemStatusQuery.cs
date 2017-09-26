using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace EPiServer.Search.Queries.Lucene
{
    public class ItemStatusQuery : GroupQuery
    {
        public ItemStatusQuery(ItemStatus status)
            : base(LuceneOperator.OR)
        {
            if ((status & ItemStatus.Approved) == ItemStatus.Approved)
                base.QueryExpressions.Add(new FieldQuery(((int)ItemStatus.Approved).ToString(CultureInfo.InvariantCulture), Field.ItemStatus));

            if ((status & ItemStatus.Pending) == ItemStatus.Pending)
                base.QueryExpressions.Add(new FieldQuery(((int)ItemStatus.Pending).ToString(CultureInfo.InvariantCulture), Field.ItemStatus));

            if ((status & ItemStatus.Removed) == ItemStatus.Removed)
                base.QueryExpressions.Add(new FieldQuery(((int)ItemStatus.Removed).ToString(CultureInfo.InvariantCulture), Field.ItemStatus));
        }
    }
}

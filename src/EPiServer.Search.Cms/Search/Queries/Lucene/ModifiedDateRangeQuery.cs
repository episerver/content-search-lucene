using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EPiServer.Search.Queries.Lucene
{
    public class ModifiedDateRangeQuery : RangeQuery
    {
        public ModifiedDateRangeQuery(DateTime start, DateTime end, bool inclusive)
            : base(Regex.Replace(start.ToString("u", CultureInfo.InvariantCulture), @"\D", ""),
            Regex.Replace(end.ToString("u", CultureInfo.InvariantCulture), @"\D", ""),
            Field.Modified, inclusive)
        {
        }
    }
}

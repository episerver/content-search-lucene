﻿using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EPiServer.Search.Queries.Lucene
{
    public class CreatedDateRangeQuery : RangeQuery
    {
        public CreatedDateRangeQuery(DateTime start, DateTime end, bool inclusive)
            : base(Regex.Replace(start.ToString("u", CultureInfo.InvariantCulture), @"\D", ""),
            Regex.Replace(end.ToString("u", CultureInfo.InvariantCulture), @"\D", ""),
            Field.Created, inclusive)
        {
        }
    }
}

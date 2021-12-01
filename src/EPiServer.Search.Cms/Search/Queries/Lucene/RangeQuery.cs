using System.Text;

namespace EPiServer.Search.Queries.Lucene
{
    public class RangeQuery : IQueryExpression
    {
        /// <summary>
        /// A Range query is used to search within literal ranges.
        /// </summary>
        /// <param name="start">The start value of the range</param>
        /// <param name="end">The end value of the range</param>
        /// <param name="field">The <see cref="Field"/> to which the range should apply</param>
        /// <param name="inclusive">Determines if the range should include or exlude the start and end values</param>
        public RangeQuery(string start, string end, Field field, bool inclusive)
        {
            Start = start;
            End = end;
            Field = field;
            Inclusive = inclusive;
        }

        /// <summary>
        /// gets and sets the range start
        /// </summary>
        public string Start
        {
            get;
            set;
        }

        /// <summary>
        /// gets and sets the range end
        /// </summary>
        public string End
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the <see cref="Field"/> for which the range should apply. 
        /// </summary>
        public Field Field
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets whether to include or exclude the start and end values from the range
        /// </summary>
        public bool Inclusive
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the text representation of this <see cref="RangeQuery"/> 
        /// </summary>
        /// <returns></returns>
        public string GetQueryExpression()
        {
            var sb = new StringBuilder();
            sb.Append(SearchSettings.GetFieldNameForField(Field));
            sb.Append(":");
            sb.Append((Inclusive ? "[" : "{"));
            sb.Append(LuceneHelpers.Escape(Start));
            sb.Append(" TO ");
            sb.Append(LuceneHelpers.Escape(End));
            sb.Append((Inclusive ? "]" : "}"));

            return sb.ToString();
        }
    }
}

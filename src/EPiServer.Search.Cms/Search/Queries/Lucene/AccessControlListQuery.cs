
namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// Representing a AccessControlListQuery for the Lucene Indexing Service with correct field name and Lucene syntax.
    /// </summary>
    public class AccessControlListQuery : CollectionQueryBase
    {
        /// <summary>
        /// Constructing a <see cref="AccessControlListQuery"/> with the default inner operator "OR"
        /// </summary>
        public AccessControlListQuery()
            : this(LuceneOperator.OR) { }

        /// <summary>
        /// Constructing a <see cref="AccessControlListQuery"/> with the passed inner <see cref="LuceneOperator"/>
        /// </summary>
        public AccessControlListQuery(LuceneOperator innerOperator) 
            : base(SearchSettings.Options.IndexingServiceFieldNameAcl, innerOperator) { }

        /// <summary>
        /// Adds the provided role to this query.
        /// </summary>
        /// <param name="roleName">The name of the role to add.</param>
        public void AddRole(string roleName)
        {
            this.Items.Add("G:" + roleName);
        }

        /// <summary>
        /// Adds the provided user to this query.
        /// </summary>
        /// <param name="userName">The username of the user to add.</param>
        public void AddUser(string userName)
        {
            this.Items.Add("U:" + userName);
        }
    }
}

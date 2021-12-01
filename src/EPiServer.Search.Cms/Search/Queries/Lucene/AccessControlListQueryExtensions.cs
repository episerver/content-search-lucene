using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// Adds acl extensions.
    /// </summary>
    public static class AccessControlListQueryExtensions
    {
        /// <summary>
        /// Adds the roles and username of the provided user to the query.
        /// </summary>
        /// <param name="query">The query to extend.</param>
        /// <param name="context">The context used for virtual roles to establish if the user is a part of a role.</param>
        public static void AddAclForUser(this AccessControlListQuery query, object context)
        {
            var queryBuilder = ServiceLocator.Current.GetInstance<IAccessControlListQueryBuilder>();
            queryBuilder.AddUser(query, PrincipalInfo.CurrentPrincipal, context);
        }
    }
}

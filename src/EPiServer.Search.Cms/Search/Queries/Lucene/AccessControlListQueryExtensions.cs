using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace EPiServer.Search.Queries.Lucene
{
    public static class AccessControlListQueryExtensions
    {
        /// <summary>
        /// Adds the roles and username of the provided user to the query.
        /// </summary>
        /// <param name="query">The query to extend.</param>
        /// <param name="principalInfo">The principal.</param>
        /// <param name="context">The context used for virtual roles to establish if the user is a part of a role.</param>
        public static void AddAclForUser(this AccessControlListQuery query, PrincipalInfo principalInfo, object context)
        {
            var queryBuilder = ServiceLocator.Current.GetInstance<IAccessControlListQueryBuilder>();
            queryBuilder.AddUser(query, principalInfo.Principal, context);
        }

      
    }
}

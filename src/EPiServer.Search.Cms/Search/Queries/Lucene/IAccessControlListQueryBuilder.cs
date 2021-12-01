using System.Security.Principal;

namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// Contains methods to build up a <see cref="AccessControlListQuery"/>
    /// </summary>
    public interface IAccessControlListQueryBuilder
    {
        /// <summary>
        /// Adds <paramref name="principal"/> including roles to <paramref name="query"/>
        /// </summary>
        void AddUser(AccessControlListQuery query, IPrincipal principal, object context);
    }
}

using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace EPiServer.Search.Queries.Lucene.Internal
{
    /// <internal-api/>
    [ServiceConfiguration(typeof(IAccessControlListQueryBuilder), IncludeServiceAccessor = false)]
    public partial class DefaultAccessControlListQueryBuilder : IAccessControlListQueryBuilder
    {
        private readonly IVirtualRoleRepository _virtualRoleRepository;

        public DefaultAccessControlListQueryBuilder(IVirtualRoleRepository virtualRoleRepository)
        {
            _virtualRoleRepository = virtualRoleRepository;
        }

        public void AddUser(AccessControlListQuery query, IPrincipal principal, object context)
        {
            AddUserInternal(query, principal, context, () =>
            {
                var roleList = (principal as ClaimsPrincipal)?.Claims.Where(c => c.Type.Equals(ClaimTypes.Role));
                if (roleList != null)
                {
                    foreach (var role in roleList)
                    {
                        query.AddRole(role.Value);
                    }
                }
            });
        }
        private void AddUserInternal(AccessControlListQuery query, IPrincipal principal, object context, Action roleHandler)
        {
            if (principal == null)
            {
                return;
            }

            // Add the username
            if (!string.IsNullOrEmpty(principal.Identity.Name))
            {
                query.AddUser(principal.Identity.Name);
            }

            // Add all roles
            roleHandler();

            // Add all virtual roles
            foreach (var roleName in _virtualRoleRepository.GetAllRoles())
            {
                if (_virtualRoleRepository.TryGetRole(roleName, out var virtualRole) && virtualRole.IsInVirtualRole(principal, context))
                {
                    query.AddRole(roleName);
                }
            }
        }
    }
}

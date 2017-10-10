using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Search.Queries.Lucene;
using EPiServer.Security;
using Moq;
using System.Security.Principal;
using Xunit;
using EPiServer.Search.Queries.Lucene.Internal;

namespace EPiServer.UnitTests.Search.Queries.Lucene
{
    public class DefaultAccessControlListQueryBuilderTest
    {

        [Fact]
        public void AddUser_WhenPrincipalIsNull_ShouldAddNothing()
        {
            AccessControlListQuery query = new AccessControlListQuery();
            var subject = new DefaultAccessControlListQueryBuilder(Mock.Of<IVirtualRoleRepository>());
            subject.AddUser(query, (IPrincipal)null, null);

            Assert.Equal(0, query.Items.Count);
        }

        [Fact]
        public void AddUser_WhenPrincipalIsSpecified_ShouldAddUserName()
        {
            string userName = "IAmUser";

            AccessControlListQuery query = new AccessControlListQuery();
            var mockVirtualRoleRepository = new Mock<IVirtualRoleRepository>();
            mockVirtualRoleRepository.Setup(r => r.GetAllRoles()).Returns(new List<string>());

            var mockPrincipal = new Mock<IPrincipal>();
            mockPrincipal.Setup(p => p.Identity.Name).Returns(userName);

            var subject = new DefaultAccessControlListQueryBuilder(mockVirtualRoleRepository.Object);

            subject.AddUser(query, mockPrincipal.Object, null);

            Assert.True(query.Items.Contains("U:" + userName));
        }

        [Fact]
        public void AddUser_WhenPrincipalIsInVirtualRole_ShouldAddRole()
        {
            string userName = "IAmUser";
            string roleName = "theRole";

            AccessControlListQuery query = new AccessControlListQuery();
            var mockVirtualRoleRepository = new Mock<IVirtualRoleRepository>();
            mockVirtualRoleRepository.Setup(r => r.GetAllRoles()).Returns(new List<string>(new[] { roleName }));

            var mockPrincipal = new Mock<IPrincipal>();
            mockPrincipal.Setup(p => p.Identity.Name).Returns(userName);

            VirtualRoleProviderBase role = new TestRole(true);
            mockVirtualRoleRepository.Setup(v => v.TryGetRole(roleName, out role)).Returns(true);

            var subject = new DefaultAccessControlListQueryBuilder(mockVirtualRoleRepository.Object);

            subject.AddUser(query, mockPrincipal.Object, null);

            Assert.True(query.Items.Contains("G:" + roleName));
        }

        [Fact]
        public void AddUser_WhenPrincipalIsNotInVirtualRole_ShouldNotAddRole()
        {
            string userName = "IAmUser";
            string roleName = "theRole";

            AccessControlListQuery query = new AccessControlListQuery();
            var mockVirtualRoleRepository = new Mock<IVirtualRoleRepository>();
            mockVirtualRoleRepository.Setup(r => r.GetAllRoles()).Returns(new List<string>(new[] { roleName }));

            var mockPrincipal = new Mock<IPrincipal>();
            mockPrincipal.Setup(p => p.Identity.Name).Returns(userName);

            VirtualRoleProviderBase role = new TestRole(false);
            mockVirtualRoleRepository.Setup(v => v.TryGetRole(roleName, out role)).Returns(true);

            var subject = new DefaultAccessControlListQueryBuilder(mockVirtualRoleRepository.Object);

            subject.AddUser(query, mockPrincipal.Object, null);

            Assert.False(query.Items.Contains("G:" + roleName));
        }

        [Fact]
        public void AddUser_WhenPrincipalHasRoleClaims_ShouldAddRoles()
        {
            string userName = "IAmUser";
            string roleName = "theRole";

            AccessControlListQuery query = new AccessControlListQuery();
            var mockVirtualRoleRepository = new Mock<IVirtualRoleRepository>();

            var principal = new GenericPrincipal(new GenericIdentity(userName), new[] { roleName });

            var subject = new DefaultAccessControlListQueryBuilder(mockVirtualRoleRepository.Object);

            subject.AddUser(query, principal, null);

            Assert.True(query.Items.Contains("G:" + roleName));
        }

        private class TestRole : VirtualRoleProviderBase
        {
            private readonly bool _isInRole;

            public TestRole(bool isInRole)
            {
                _isInRole = isInRole;
            }
            public override bool IsInVirtualRole(IPrincipal principal, object context)
            {
                return _isInRole;
            }
        }
    }
}

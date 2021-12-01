using System.Collections.Generic;
using System.Security.Principal;
using EPiServer.Search.Queries.Lucene;
using EPiServer.Search.Queries.Lucene.Internal;
using EPiServer.Security;
using Moq;
using Xunit;

namespace EPiServer.UnitTests.Search.Queries.Lucene
{
    public class DefaultAccessControlListQueryBuilderTest
    {

        [Fact]
        public void AddUser_WhenPrincipalIsNull_ShouldAddNothing()
        {
            var query = new AccessControlListQuery();
            var subject = new DefaultAccessControlListQueryBuilder(Mock.Of<IVirtualRoleRepository>());
            subject.AddUser(query, null, null);

            Assert.Equal(0, query.Items.Count);
        }

        [Fact]
        public void AddUser_WhenPrincipalIsSpecified_ShouldAddUserName()
        {
            var userName = "IAmUser";

            var query = new AccessControlListQuery();
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
            var userName = "IAmUser";
            var roleName = "theRole";

            var query = new AccessControlListQuery();
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
            var userName = "IAmUser";
            var roleName = "theRole";

            var query = new AccessControlListQuery();
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
            var userName = "IAmUser";
            var roleName = "theRole";

            var query = new AccessControlListQuery();
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
            public override bool IsInVirtualRole(IPrincipal principal, object context) => _isInRole;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using Catalyst.Fabric.Authorization.Models;
using Catalyst.Fabric.Authorization.Models.Requests;
using Xunit;

namespace Catalyst.Fabric.Authorization.Client.FunctionalTests
{
    [Collection(FunctionalTestConstants.FunctionTestTitle)]
    public class GroupTests : BaseTest

    {
        public GroupTests(FunctionalTestFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async void AddAndGetGroup_ValidRequest_Success()
        {
            var accessToken = await fixture.GetAccessTokenForAuthClient();

            var groupName = Guid.NewGuid().ToString();
            var group = await _authorizationClient.AddGroup(accessToken, new GroupRoleApiModel
            {
                GroupName = groupName,
                GroupSource = "Windows"
            });

            Assert.NotNull(group);

            var groupId = group.Id;

            group = await _authorizationClient.GetGroup(accessToken, groupName);
            Assert.Equal(groupId, group.Id);
            Assert.Equal(groupName, group.GroupName);
            Assert.Equal("Windows", group.GroupSource);
        }

        [Fact]
        public async void AddGroup_InvalidRequest_Exception()
        {
            try
            {
                var accessToken = await fixture.GetAccessTokenForAuthClient();
                await _authorizationClient.AddGroup(accessToken, new GroupRoleApiModel());
            }
            catch (AuthorizationException e)
            {
                Assert.Equal(e.Details.Code, HttpStatusCode.BadRequest.ToString());
                Assert.Equal("Please specify a Name for this Group.", e.Details.Message);
            }
        }

        [Fact]
        public async void AddAndRemoveGroupRole_ValidRequest_Success()
        {
            var accessToken = await fixture.GetAccessTokenForAuthClient();

            // create group
            var groupName = Guid.NewGuid().ToString();
            var group = await _authorizationClient.AddGroup(accessToken, new GroupRoleApiModel
            {
                GroupName = groupName,
                GroupSource = "Windows"
            });

            Assert.NotNull(group);

            // create role
            var roleName = Guid.NewGuid().ToString();
            var role = await _authorizationClient.AddRole(accessToken, new RoleApiModel
            {
                Grain = "app",
                SecurableItem = FunctionalTestConstants.IdentityTestUser,
                Name = roleName
            });

            Assert.NotNull(role);
            Assert.NotNull(role.Id);

            // add role to group
            group = await _authorizationClient.AddRolesToGroup(accessToken, groupName, new List<RoleApiModel>
            {
                role
            });

            Assert.Contains(group.Roles, r => r.Name == role.Name);

            // remove role from group
            group = await _authorizationClient.DeleteRolesFromGroup(accessToken, groupName, new List<RoleIdentifierApiRequest>
            {
                new RoleIdentifierApiRequest
                {
                    RoleId = role.Id.Value
                }
            });

            Assert.Empty(group.Roles);
        }

        [Fact]
        public async void AddAndRemoveGroupUser_ValidRequest_Success()
        {
            var accessToken = await fixture.GetAccessTokenForAuthClient();

            // create group
            var groupName = Guid.NewGuid().ToString();
            var group = await _authorizationClient.AddGroup(accessToken, new GroupRoleApiModel
            {
                GroupName = groupName,
                GroupSource = "Custom"
            });

            Assert.NotNull(group);

            // create user
            const string identityProvider = "windows";
            var subjectId = Guid.NewGuid().ToString();
            var user = await _authorizationClient.AddUser(accessToken, new UserApiModel
            {
                IdentityProvider = identityProvider,
                SubjectId = subjectId
            });

            Assert.NotNull(user);

            // add user to group
            var groupUserApiModel = await _authorizationClient.AddUsersToGroup(accessToken, groupName, new List<UserIdentifierApiRequest>
            {
                new UserIdentifierApiRequest
                {
                    IdentityProvider = identityProvider,
                    SubjectId = subjectId
                }
            });

            Assert.Contains(groupUserApiModel.Users, u => u.IdentityProvider == user.IdentityProvider
                                                          && u.SubjectId == user.SubjectId);

            // remove user from group
            groupUserApiModel = await _authorizationClient.DeleteUserFromGroup(accessToken, groupName,
                new GroupUserRequest
                {
                    IdentityProvider = identityProvider,
                    SubjectId = subjectId
                });

            Assert.Empty(groupUserApiModel.Users);
        }
    }
}
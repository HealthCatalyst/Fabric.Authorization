using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fabric.Authorization.Client.Extensions;
using Fabric.Authorization.Client.Routes;
using Fabric.Authorization.Models;
using Fabric.Authorization.Models.Requests;
using Newtonsoft.Json;

namespace Fabric.Authorization.Client
{
    public class AuthorizationClient
    {
        
        private readonly HttpClient client;

        public AuthorizationClient(HttpClient client)
        {
            this.client = client;
        }

        #region Users

        public async Task<UserApiModel> AddUser(string accessToken, UserApiModel userModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new UserRouteBuilder().Route)
                .AddContent(userModel)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<UserApiModel> GetPermissionsForCurrentUser(string accessToken)
        {
            var route = new UserRouteBuilder().UserPermissionsRoute;
            var message = new HttpRequestMessage(HttpMethod.Get, route).AddBearerToken(accessToken);

            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<List<PermissionApiModel>> GetUserPermissions(string accessToken, string identityProvider, string subjectId)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                    new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserPermissionsRoute)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<List<PermissionApiModel>>(message).ConfigureAwait(false);
        }

        public async Task AddPermissionsToUser(string accessToken, string identityProvider, string subjectId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserPermissionsRoute)
                .AddContent(permissionModels)
                .AddBearerToken(accessToken);

            await SendRequest(message).ConfigureAwait(false);
        }

        public async Task DeletePermissionsFromUser(string accessToken, string identityProvider, string subjectId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserPermissionsRoute)
                .AddContent(permissionModels)
                .AddBearerToken(accessToken);

            await SendRequest(message).ConfigureAwait(false);
        }

        public async Task<UserApiModel> AddRolesToUser(string accessToken, string identityProvider, string subjectId, List<RoleApiModel> roleModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                    new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserRolesRoute)
                .AddContent(roleModels)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<UserApiModel> DeleteRolesFromUser(string accessToken, string identityProvider, string subjectId, List<RoleApiModel> roleModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserRolesRoute)
                .AddContent(roleModels)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        #endregion


        #region Clients

        public async Task<ClientApiModel> AddClient(string accessToken, ClientApiModel clientModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new ClientRouteBuilder().Route)
                .AddContent(clientModel)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<ClientApiModel>(message).ConfigureAwait(false);
        }

        public async Task<ClientApiModel> GetClient(string accessToken, string clientId)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new ClientRouteBuilder().ClientId(clientId).Route)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<ClientApiModel>(message).ConfigureAwait(false);
        }

        #endregion

        #region Roles

        public async Task<RoleApiModel> AddRole(string accessToken, RoleApiModel roleModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new RoleRouteBuilder().Route)
                .AddContent(roleModel)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<RoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<RoleApiModel> AddPermissionToRole(string accessToken, string roleId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new RoleRouteBuilder().RoleId(roleId).Route)
                .AddContent(permissionModels)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<RoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<RoleApiModel> DeletePermissionsFromRole(string accessToken, string roleId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, new RoleRouteBuilder().RoleId(roleId).Route)
                .AddContent(permissionModels)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<RoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetRole(string accessToken, string roleId)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new RoleRouteBuilder().RoleId(roleId).Route)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<List<RoleApiModel>>(message).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetRole(string accessToken, string grain, string securableItem, string roleName = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                    new RoleRouteBuilder().Grain(grain).SecurableItem(securableItem).Name(roleName).Route)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<List<RoleApiModel>>(message).ConfigureAwait(false);
        }

        #endregion

        #region Permissions

        public async Task<PermissionApiModel> AddPermission(string accessToken, PermissionApiModel permissionModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new PermissionRouteBuilder().Route)
                .AddContent(permissionModel)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<PermissionApiModel>(message).ConfigureAwait(false);
        }

        public async Task<PermissionApiModel> GetPermission(string accessToken, string permissionId)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                    new PermissionRouteBuilder().PermissionId(permissionId).Route)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<PermissionApiModel>(message).ConfigureAwait(false);
        }

        public async Task<List<PermissionApiModel>> GetPermissions(string accessToken, string grain, string securableItem, string permissionName = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                    new PermissionRouteBuilder().Grain(grain).SecurableItem(securableItem).Name(permissionName).Route)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<List<PermissionApiModel>>(message).ConfigureAwait(false);
        }

        #endregion

        #region Groups

        public async Task<GroupRoleApiModel> GetGroup(string accessToken, string groupName)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new GroupRouteBuilder().Name(groupName).Route)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetGroupRoles(string accessToken, string groupName)
        {
            var message =
                new HttpRequestMessage(HttpMethod.Get, new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
                    .AddBearerToken(accessToken);

            return await SendAndParseJson<List<RoleApiModel>>(message).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetGroupRoles(string accessToken, string groupName, string grain, string securableItem)
        {
            var message =
                new HttpRequestMessage(HttpMethod.Get, new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
                    .AddBearerToken(accessToken);

            return await SendAndParseJson<List<RoleApiModel>>(message).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> AddGroup(string accessToken, GroupRoleApiModel groupModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new GroupRouteBuilder().Route)
                .AddContent(groupModel)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> AddRolesToGroup(string accessToken, string groupName, List<RoleApiModel> roleModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                    new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
                .AddContent(roleModels)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> DeleteRolesFromGroup(string accessToken, string groupName, List<RoleIdentifierApiRequest> roleIds)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
                .AddContent(roleIds)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupUserApiModel> AddUsersToGroup(string accessToken, string groupName, List<UserIdentifierApiRequest> userIds)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                    new GroupRouteBuilder().Name(groupName).GroupUsersRoute)
                .AddContent(userIds)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupUserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupUserApiModel> DeleteUserFromGroup(string accessToken, string groupName, GroupUserRequest user)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new GroupRouteBuilder().Name(groupName).GroupUsersRoute)
                .AddContent(user)
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupUserApiModel>(message).ConfigureAwait(false);
        }

        #endregion

        private async Task<T> SendAndParseJson<T>(HttpRequestMessage message)
        {
            var response = await client.SendAsync(message).ConfigureAwait(false);
            var stringResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(stringResponse);
            }

            var error = JsonConvert.DeserializeObject<Error>(stringResponse);
            throw new AuthorizationException(error);
        }

        private async Task SendRequest(HttpRequestMessage message)
        {
            var response = await client.SendAsync(message).ConfigureAwait(false);
            var stringResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = JsonConvert.DeserializeObject<Error>(stringResponse);
                throw new AuthorizationException(error);
            }
        }
    }
}
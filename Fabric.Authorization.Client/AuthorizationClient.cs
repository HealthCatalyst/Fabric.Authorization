using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

        public async Task<UserApiModel> AddUser(UserApiModel userModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new UserRouteBuilder().Route)
            {
                Content = new StringContent(JsonConvert.SerializeObject(userModel), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<UserApiModel> GetPermissionsForCurrentUser()
        {
            var route = new UserRouteBuilder().UserPermissionsRoute;
            var message = new HttpRequestMessage(HttpMethod.Get, route);
            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<List<PermissionApiModel>> GetUserPermissions(string identityProvider, string subjectId)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserPermissionsRoute);
            return await SendAndParseJson<List<PermissionApiModel>>(message).ConfigureAwait(false);
        }

        public async Task AddPermissionsToUser(string identityProvider, string subjectId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserPermissionsRoute)
            {
                Content = new StringContent(JsonConvert.SerializeObject(permissionModels), Encoding.UTF8, "application/json")
            };

            await SendRequest(message).ConfigureAwait(false);
        }

        public async Task DeletePermissionsFromUser(string identityProvider, string subjectId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserPermissionsRoute)
            {
                Content = new StringContent(JsonConvert.SerializeObject(permissionModels), Encoding.UTF8, "application/json")
            };

            await SendRequest(message).ConfigureAwait(false);
        }

        public async Task<UserApiModel> AddRolesToUser(string identityProvider, string subjectId, List<RoleApiModel> roleModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserRolesRoute)
            {
                Content = new StringContent(JsonConvert.SerializeObject(roleModels), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<UserApiModel> DeleteRolesFromUser(string identityProvider, string subjectId, List<RoleApiModel> roleModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserRolesRoute)
            {
                Content = new StringContent(JsonConvert.SerializeObject(roleModels), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        #endregion


        #region Clients

        public async Task<ClientApiModel> AddClient(ClientApiModel clientModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new ClientRouteBuilder().Route)
            {
                Content = new StringContent(JsonConvert.SerializeObject(clientModel), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<ClientApiModel>(message).ConfigureAwait(false);
        }

        public async Task<ClientApiModel> GetClient(string clientId)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new ClientRouteBuilder().ClientId(clientId).Route);
            return await SendAndParseJson<ClientApiModel>(message).ConfigureAwait(false);
        }

        #endregion

        #region Roles

        public async Task<RoleApiModel> AddRole(RoleApiModel roleModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new RoleRouteBuilder().Route)
            {
                Content = new StringContent(JsonConvert.SerializeObject(roleModel), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<RoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<RoleApiModel> AddPermissionToRole(string roleId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new RoleRouteBuilder().RoleId(roleId).Route)
            {
                Content = new StringContent(JsonConvert.SerializeObject(permissionModels), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<RoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<RoleApiModel> DeletePermissionsFromRole(string roleId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, new RoleRouteBuilder().RoleId(roleId).Route)
            {
                Content = new StringContent(JsonConvert.SerializeObject(permissionModels), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<RoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetRole(string roleId)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new RoleRouteBuilder().RoleId(roleId).Route);
            return await SendAndParseJson<List<RoleApiModel>>(message).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetRole(string grain, string securableItem, string roleName = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                new RoleRouteBuilder().Grain(grain).SecurableItem(securableItem).Name(roleName).Route);
            return await SendAndParseJson<List<RoleApiModel>>(message).ConfigureAwait(false);
        }

        #endregion

        #region Permissions

        public async Task<PermissionApiModel> AddPermission(PermissionApiModel permissionModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new PermissionRouteBuilder().Route)
            {
                Content = new StringContent(JsonConvert.SerializeObject(permissionModel), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<PermissionApiModel>(message).ConfigureAwait(false);
        }

        public async Task<PermissionApiModel> GetPermission(string permissionId)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new PermissionRouteBuilder().PermissionId(permissionId).Route);
            return await SendAndParseJson<PermissionApiModel>(message).ConfigureAwait(false);
        }

        public async Task<List<PermissionApiModel>> GetPermissions(string grain, string securableItem, string permissionName = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                new PermissionRouteBuilder().Grain(grain).SecurableItem(securableItem).Name(permissionName).Route);
            return await SendAndParseJson<List<PermissionApiModel>>(message).ConfigureAwait(false);
        }

        #endregion

        #region Groups

        public async Task<GroupRoleApiModel> GetGroup(string groupName)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new GroupRouteBuilder().Name(groupName).Route);
            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetGroupRoles(string groupName)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new GroupRouteBuilder().Name(groupName).GroupRolesRoute);
            return await SendAndParseJson<List<RoleApiModel>>(message).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetGroupRoles(string groupName, string grain, string securableItem)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new GroupRouteBuilder().Name(groupName).GroupRolesRoute);
            return await SendAndParseJson<List<RoleApiModel>>(message).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> AddGroup(GroupRoleApiModel groupModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new GroupRouteBuilder().Route)
            {
                Content = new StringContent(JsonConvert.SerializeObject(groupModel), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> AddRolesToGroup(string groupName, List<RoleApiModel> roleModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
            {
                Content = new StringContent(JsonConvert.SerializeObject(roleModels), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> DeleteRolesFromGroup(string groupName, List<RoleIdentifierApiRequest> roleIds)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
            {
                Content = new StringContent(JsonConvert.SerializeObject(roleIds), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupUserApiModel> AddUsersToGroup(string groupName, List<UserIdentifierApiRequest> userIds)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new GroupRouteBuilder().Name(groupName).GroupUsersRoute)
            {
                Content = new StringContent(JsonConvert.SerializeObject(userIds), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<GroupUserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupUserApiModel> DeleteUserFromGroup(string groupName, GroupUserRequest user)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, new GroupRouteBuilder().Name(groupName).GroupUsersRoute)
            {
                Content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json")
            };

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
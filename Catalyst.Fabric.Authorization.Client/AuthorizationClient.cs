using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Catalyst.Fabric.Authorization.Client.Extensions;
using Catalyst.Fabric.Authorization.Client.Routes;
using Catalyst.Fabric.Authorization.Models;
using Catalyst.Fabric.Authorization.Models.Requests;
using Newtonsoft.Json;

namespace Catalyst.Fabric.Authorization.Client
{
    public class AuthorizationClient
    {
        
        private readonly HttpClient _client;

        public AuthorizationClient(HttpClient client)
        {
            this._client = client.FormatBaseUrl();
        }
        
        #region Users

        public async Task<UserApiModel> AddUser(string accessToken, UserApiModel userModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new UserRouteBuilder().Route)
                .AddContent(userModel)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<UserPermissionsApiModel> GetPermissionsForCurrentUser(string accessToken, string grain = "", string securableItem = "")
        {
            CheckIfStringNullOrEmpty(accessToken, nameof(accessToken));

            var route = new UserRouteBuilder()
                            .Grain(grain)
                            .SecurableItem(securableItem)
                            .UserPermissionsRoute;
            var message = new HttpRequestMessage(HttpMethod.Get, route)
                .AddBearerToken(accessToken)
                .AddAcceptHeader();

            return await SendAndParseJson<UserPermissionsApiModel>(message).ConfigureAwait(false);
        }

        public async Task<bool> DoesUserHavePermission(string accessToken, string permission)
        {
            CheckIfStringNullOrEmpty(accessToken, nameof(accessToken));
            CheckIfStringNullOrEmpty(permission, nameof(permission));

            var userPermissions = await this.GetPermissionsForCurrentUser(accessToken).ConfigureAwait(false);
            return DoesUserHavePermission(userPermissions, permission);
        }

        public bool DoesUserHavePermission(UserPermissionsApiModel userPermissions, string permission)
        {
            if(userPermissions == null)
            {
                return false;
            }

            CheckIfStringNullOrEmpty(permission, nameof(permission));

            return userPermissions.Permissions.Any(p => p == permission);
        }

        public async Task<List<PermissionApiModel>> GetUserPermissions(string accessToken, string identityProvider, string subjectId)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                    new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserPermissionsRoute)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<List<PermissionApiModel>>(message).ConfigureAwait(false);
        }

        public async Task AddPermissionsToUser(string accessToken, string identityProvider, string subjectId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserPermissionsRoute)
                .AddContent(permissionModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            await SendRequest(message).ConfigureAwait(false);
        }

        public async Task DeletePermissionsFromUser(string accessToken, string identityProvider, string subjectId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserPermissionsRoute)
                .AddContent(permissionModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            await SendRequest(message).ConfigureAwait(false);
        }

        public async Task<UserApiModel> AddRolesToUser(string accessToken, string identityProvider, string subjectId, List<RoleApiModel> roleModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                    new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserRolesRoute)
                .AddContent(roleModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<UserApiModel> DeleteRolesFromUser(string accessToken, string identityProvider, string subjectId, List<RoleApiModel> roleModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserRolesRoute)
                .AddContent(roleModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        #endregion

        #region Clients

        public async Task<ClientApiModel> AddClient(string accessToken, ClientApiModel clientModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new ClientRouteBuilder().Route)
                .AddContent(clientModel)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<ClientApiModel>(message).ConfigureAwait(false);
        }

        public async Task<ClientApiModel> GetClient(string accessToken, string clientId)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new ClientRouteBuilder().ClientId(clientId).Route)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<ClientApiModel>(message).ConfigureAwait(false);
        }

        #endregion

        #region Roles

        public async Task<RoleApiModel> AddRole(string accessToken, RoleApiModel roleModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new RoleRouteBuilder().Route)
                .AddContent(roleModel)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<RoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<RoleApiModel> AddPermissionToRole(string accessToken, string roleId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new RoleRouteBuilder().RoleId(roleId).RolePermissionsRoute)
                .AddContent(permissionModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<RoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<RoleApiModel> DeletePermissionsFromRole(string accessToken, string roleId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, new RoleRouteBuilder().RoleId(roleId).RolePermissionsRoute)
                .AddContent(permissionModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<RoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetRole(string accessToken, string grain, string securableItem, string roleName = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                    new RoleRouteBuilder().Grain(grain).SecurableItem(securableItem).Name(roleName).Route)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<List<RoleApiModel>>(message).ConfigureAwait(false);
        }

        #endregion

        #region Permissions

        public async Task<PermissionApiModel> AddPermission(string accessToken, PermissionApiModel permissionModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new PermissionRouteBuilder().Route)
                .AddContent(permissionModel)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<PermissionApiModel>(message).ConfigureAwait(false);
        }

        public async Task<PermissionApiModel> GetPermission(string accessToken, string permissionId)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                    new PermissionRouteBuilder().PermissionId(permissionId).Route)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<PermissionApiModel>(message).ConfigureAwait(false);
        }

        public async Task<List<PermissionApiModel>> GetPermissions(string accessToken, string grain, string securableItem, string permissionName = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                    new PermissionRouteBuilder().Grain(grain).SecurableItem(securableItem).Name(permissionName).Route)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<List<PermissionApiModel>>(message).ConfigureAwait(false);
        }

        #endregion

        #region Groups

        public async Task<GroupRoleApiModel> GetGroup(string accessToken, string groupName)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new GroupRouteBuilder().Name(groupName).Route)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetGroupRoles(string accessToken, string groupName)
        {
            var message =
                new HttpRequestMessage(HttpMethod.Get, new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
                    .AddAcceptHeader()
                    .AddBearerToken(accessToken);

            return await SendAndParseJson<List<RoleApiModel>>(message).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetGroupRoles(string accessToken, string groupName, string grain, string securableItem)
        {
            var message =
                new HttpRequestMessage(HttpMethod.Get, new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
                    .AddAcceptHeader()
                    .AddBearerToken(accessToken);

            return await SendAndParseJson<List<RoleApiModel>>(message).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> AddGroup(string accessToken, GroupRoleApiModel groupModel)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new GroupRouteBuilder().Route)
                .AddContent(groupModel)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> AddRolesToGroup(string accessToken, string groupName, List<RoleApiModel> roleModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                    new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
                .AddContent(roleModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> DeleteRolesFromGroup(string accessToken, string groupName, List<RoleIdentifierApiRequest> roleIds)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
                .AddContent(roleIds)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupUserApiModel> AddUsersToGroup(string accessToken, string groupName, List<UserIdentifierApiRequest> userIds)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                    new GroupRouteBuilder().Name(groupName).GroupUsersRoute)
                .AddContent(userIds)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupUserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupUserApiModel> DeleteUserFromGroup(string accessToken, string groupName, GroupUserRequest user)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new GroupRouteBuilder().Name(groupName).GroupUsersRoute)
                .AddContent(user)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupUserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> AddChildGroups(string accessToken, string groupName, List<GroupIdentifierApiRequest> groupIds)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                    new GroupRouteBuilder().Name(groupName).ChildGroupsRoute)
                .AddContent(groupIds)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> DeleteChildGroups(string accessToken, string groupName, List<GroupIdentifierApiRequest> groupIds)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new GroupRouteBuilder().Name(groupName).ChildGroupsRoute)
                .AddContent(groupIds)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message).ConfigureAwait(false);
        }

        #endregion

        private void CheckIfStringNullOrEmpty(string value, string name)
        {
            if(string.IsNullOrEmpty(value))
            {
                var errorMessage = new Error()
                {
                     Message = $"Value {name} cannot be null or empty."
                };

                throw new AuthorizationException(errorMessage);
            }
        }

        private void CheckIfNull(object value, string name)
        {
            if (value == null)
            {
                var errorMessage = new Error()
                {
                    Message = $"Value {name} cannot be null or empty."
                };

                throw new AuthorizationException(errorMessage);
            }
        }

        private async Task<T> SendAndParseJson<T>(HttpRequestMessage message)
        {
            var response = await _client.SendAsync(message).ConfigureAwait(false);
            var stringResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(stringResponse);
            }

            try
            {
                var error = JsonConvert.DeserializeObject<Error>(stringResponse);

                error = error ?? new Error
                {
                    Message = stringResponse,
                    Code = response.StatusCode.ToString()
                };

                throw new AuthorizationException(error);
            }
            catch (JsonReaderException)
            {
                var error = new Error
                {
                    Code = response.StatusCode.ToString(),
                    Message = stringResponse
                };
                throw new AuthorizationException(error);
            }
            catch(JsonSerializationException)
            {
                var error = new Error
                {
                     Code = response.StatusCode.ToString(),
                     Message = stringResponse
                };
                throw new AuthorizationException(error);
            }
        }

        private async Task SendRequest(HttpRequestMessage message)
        {
            var response = await _client.SendAsync(message).ConfigureAwait(false);
            var stringResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode < HttpStatusCode.InternalServerError)
                {
                    var error = JsonConvert.DeserializeObject<Error>(stringResponse);
                    throw new AuthorizationException(error);
                }

                throw new AuthorizationException(new Error
                {
                    Code = response.StatusCode.ToString(),
                    Message = response.ReasonPhrase
                });
            }
        }
    }
}
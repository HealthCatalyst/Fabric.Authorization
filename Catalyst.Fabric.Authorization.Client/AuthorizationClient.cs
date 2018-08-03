using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Fabric.Authorization.Client.Extensions;
using Catalyst.Fabric.Authorization.Client.Routes;
using Catalyst.Fabric.Authorization.Models;
using Catalyst.Fabric.Authorization.Models.Requests;
using Newtonsoft.Json;
using Polly;

namespace Catalyst.Fabric.Authorization.Client
{
    public class AuthorizationClient
    {
        private Policy RetryPolicy;
        private Policy CircuitBreakerPolicy;

        private int MaxRetryAttempts;

        private int TimeSpanInMinutes;

        private int NumberOfErrorsBeforeCircuitBreak;

        private readonly HttpClient _client;

        public AuthorizationClient(HttpClient client, bool useCircuitBreaker = false, bool useRetry = false)
        {
            this._client = client.FormatBaseUrl();
            
            if(useCircuitBreaker)
            {
                this.InitializeCircuitBreaker();
            }

            if(useRetry)
            {
                this.InitializeRetry();
            }
        }

        public AuthorizationClient(HttpClient client, bool useCircuitBreaker = false, bool useRetry = false, int maxRetryAttempts = 5, int timeSpanInMinutes = 1, int numberOfErrorsBeforeCircuitBreak = 200)
            : this(client, useCircuitBreaker, useRetry)
        {
            this.MaxRetryAttempts = maxRetryAttempts;
            this.TimeSpanInMinutes = timeSpanInMinutes;
            this.NumberOfErrorsBeforeCircuitBreak = numberOfErrorsBeforeCircuitBreak;
        }

        private void InitializeCircuitBreaker()
        {
            // if there is any Authorization Exception,
            // AggregrateException or OptionationCanceledException,
            // and there are 200 in one minute, it will trigger the
            // circuit breaker;
            this.CircuitBreakerPolicy = Policy.Handle<OperationCanceledException>()
                .Or<AuthorizationException>()
                .Or<OperationCanceledException>()
                .CircuitBreakerAsync(this.NumberOfErrorsBeforeCircuitBreak, TimeSpan.FromMinutes(this.TimeSpanInMinutes));
        }

        private void InitializeRetry()
        {
            // if the exception is a 500 server error of any kind,
            // do a retry just to make sure.
            // Everything else, just fail.
            // if there are too many 500 errors, then circuit break.
            this.RetryPolicy = Policy.Handle<OperationCanceledException>()
                .Or<AuthorizationException>(auth => auth.Details.Code.First() == '5' && auth.Details.Code.Length == 3)
                .Or<OperationCanceledException>()
                .WaitAndRetryAsync(this.MaxRetryAttempts, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        #region Users

        public async Task<UserApiModel> AddUser(string accessToken, UserApiModel userModel, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new UserRouteBuilder().Route)
                .AddContent(userModel)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<UserApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<UserPermissionsApiModel> GetPermissionsForCurrentUser(string accessToken, string grain = "", string securableItem = "", CancellationToken? token = null)
        {
            CheckIfStringNullOrEmpty(accessToken, nameof(accessToken));

            var route = new UserRouteBuilder()
                            .Grain(grain)
                            .SecurableItem(securableItem)
                            .UserPermissionsRoute;
            var message = new HttpRequestMessage(HttpMethod.Get, route)
                .AddBearerToken(accessToken)
                .AddAcceptHeader();

            return await SendAndParseJson<UserPermissionsApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<bool> DoesUserHavePermission(string accessToken, string permission, CancellationToken? token = null)
        {
            CheckIfStringNullOrEmpty(accessToken, nameof(accessToken));
            CheckIfStringNullOrEmpty(permission, nameof(permission));

            var userPermissions = await this.GetPermissionsForCurrentUser(accessToken, token: token).ConfigureAwait(false);
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

        public async Task<List<PermissionApiModel>> GetUserPermissions(string accessToken, string identityProvider, string subjectId, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                    new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserPermissionsRoute)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<List<PermissionApiModel>>(message, token).ConfigureAwait(false);
        }

        public async Task AddPermissionsToUser(string accessToken, string identityProvider, string subjectId, List<PermissionApiModel> permissionModels, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserPermissionsRoute)
                .AddContent(permissionModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            await SendRequest(message, token).ConfigureAwait(false);
        }

        public async Task DeletePermissionsFromUser(string accessToken, string identityProvider, string subjectId, List<PermissionApiModel> permissionModels, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserPermissionsRoute)
                .AddContent(permissionModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            await SendRequest(message, token).ConfigureAwait(false);
        }

        public async Task<UserApiModel> AddRolesToUser(string accessToken, string identityProvider, string subjectId, List<RoleApiModel> roleModels, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                    new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserRolesRoute)
                .AddContent(roleModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<UserApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<UserApiModel> DeleteRolesFromUser(string accessToken, string identityProvider, string subjectId, List<RoleApiModel> roleModels, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new UserRouteBuilder().IdentityProvider(identityProvider).SubjectId(subjectId).UserRolesRoute)
                .AddContent(roleModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<UserApiModel>(message, token).ConfigureAwait(false);
        }

        #endregion

        #region Clients

        public async Task<ClientApiModel> AddClient(string accessToken, ClientApiModel clientModel, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new ClientRouteBuilder().Route)
                .AddContent(clientModel)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<ClientApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<ClientApiModel> GetClient(string accessToken, string clientId, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new ClientRouteBuilder().ClientId(clientId).Route)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<ClientApiModel>(message, token).ConfigureAwait(false);
        }

        #endregion

        #region Roles

        public async Task<RoleApiModel> AddRole(string accessToken, RoleApiModel roleModel, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new RoleRouteBuilder().Route)
                .AddContent(roleModel)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<RoleApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<RoleApiModel> AddPermissionToRole(string accessToken, string roleId, List<PermissionApiModel> permissionModels, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new RoleRouteBuilder().RoleId(roleId).RolePermissionsRoute)
                .AddContent(permissionModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<RoleApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<RoleApiModel> DeletePermissionsFromRole(string accessToken, string roleId, List<PermissionApiModel> permissionModels, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, new RoleRouteBuilder().RoleId(roleId).RolePermissionsRoute)
                .AddContent(permissionModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<RoleApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetRole(string accessToken, string grain, string securableItem, string roleName = null, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                    new RoleRouteBuilder().Grain(grain).SecurableItem(securableItem).Name(roleName).Route)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<List<RoleApiModel>>(message, token).ConfigureAwait(false);
        }

        #endregion

        #region Permissions

        public async Task<PermissionApiModel> AddPermission(string accessToken, PermissionApiModel permissionModel, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new PermissionRouteBuilder().Route)
                .AddContent(permissionModel)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<PermissionApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<PermissionApiModel> GetPermission(string accessToken, string permissionId, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                    new PermissionRouteBuilder().PermissionId(permissionId).Route)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<PermissionApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<List<PermissionApiModel>> GetPermissions(string accessToken, string grain, string securableItem, string permissionName = null, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                    new PermissionRouteBuilder().Grain(grain).SecurableItem(securableItem).Name(permissionName).Route)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<List<PermissionApiModel>>(message, token).ConfigureAwait(false);
        }

        #endregion

        #region Groups

        public async Task<GroupRoleApiModel> GetGroup(string accessToken, string groupName, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new GroupRouteBuilder().Name(groupName).Route)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetGroupRoles(string accessToken, string groupName, CancellationToken? token = null)
        {
            var message =
                new HttpRequestMessage(HttpMethod.Get, new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
                    .AddAcceptHeader()
                    .AddBearerToken(accessToken);

            return await SendAndParseJson<List<RoleApiModel>>(message, token).ConfigureAwait(false);
        }

        public async Task<List<RoleApiModel>> GetGroupRoles(string accessToken, string groupName, string grain, string securableItem, CancellationToken? token = null)
        {
            var message =
                new HttpRequestMessage(HttpMethod.Get, new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
                    .AddAcceptHeader()
                    .AddBearerToken(accessToken);

            return await SendAndParseJson<List<RoleApiModel>>(message, token).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> AddGroup(string accessToken, GroupRoleApiModel groupModel, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, new GroupRouteBuilder().Route)
                .AddContent(groupModel)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> AddRolesToGroup(string accessToken, string groupName, List<RoleApiModel> roleModels, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                    new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
                .AddContent(roleModels)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<GroupRoleApiModel> DeleteRolesFromGroup(string accessToken, string groupName, List<RoleIdentifierApiRequest> roleIds, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new GroupRouteBuilder().Name(groupName).GroupRolesRoute)
                .AddContent(roleIds)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupRoleApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<GroupUserApiModel> AddUsersToGroup(string accessToken, string groupName, List<UserIdentifierApiRequest> userIds, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post,
                    new GroupRouteBuilder().Name(groupName).GroupUsersRoute)
                .AddContent(userIds)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupUserApiModel>(message, token).ConfigureAwait(false);
        }

        public async Task<GroupUserApiModel> DeleteUserFromGroup(string accessToken, string groupName, GroupUserRequest user, CancellationToken? token = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete,
                    new GroupRouteBuilder().Name(groupName).GroupUsersRoute)
                .AddContent(user)
                .AddAcceptHeader()
                .AddBearerToken(accessToken);

            return await SendAndParseJson<GroupUserApiModel>(message, token).ConfigureAwait(false);
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

        private async Task<T> SendAndParseJson<T>(HttpRequestMessage message, CancellationToken? token)
        {
            // This function wraps all the retry/circuit breaker logic
            // for the sending of a request.
            
            // retry first a few times.
            var RetryTask = RetryPolicy.ExecuteAsync<T>(
                () => this.SendAndParseJsonImpl<T>(message, token));
            
            // if the retry doesnt work, create circuit breaker
            // logic
            return await CircuitBreakerPolicy.ExecuteAsync<T>(
                async () => await RetryTask
            );
        }

        private async Task<T> SendAndParseJsonImpl<T>(HttpRequestMessage message, CancellationToken? token)
        {
            var response = new HttpResponseMessage();
            if(token.HasValue)
            {
                response = await _client.SendAsync(message).ConfigureAwait(false);
            }
            else
            {
                response = await _client.SendAsync(message, token.Value).ConfigureAwait(false);   
            }
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

        private async Task SendRequest(HttpRequestMessage message, CancellationToken? token)
        {
            // This function wraps all the retry/circuit breaker logic
            // for the sending of a request.

            // retry first a few times.
            var RetryTask = RetryPolicy.ExecuteAsync(
                () => this.SendRequestImpl(message, token));
            
            // if the retry doesnt work, create circuit breaker
            // logic
            await CircuitBreakerPolicy.ExecuteAsync(
                async () => await RetryTask
            );
        }

        private async Task SendRequestImpl(HttpRequestMessage message, CancellationToken? token)
        {
            var response = new HttpResponseMessage();
            if(token.HasValue)
            {
                response = await _client.SendAsync(message).ConfigureAwait(false);
            }
            else
            {
                response = await _client.SendAsync(message, token.Value).ConfigureAwait(false);   
            }
            
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
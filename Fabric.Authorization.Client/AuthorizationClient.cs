using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Authorization.Client.Routes;
using Fabric.Authorization.Models;
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


        #region Clients
        public async Task<UserApiModel> GetPermissionsForCurrentUser()
        {
            var route = new UserRouteBuilder().UserPermissionsRoute;
            var message = new HttpRequestMessage(HttpMethod.Get, route);
            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

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
            var message = new HttpRequestMessage(HttpMethod.Get, new ClientRouteBuilder().ClientId(clientId).ToString());
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
            var message = new HttpRequestMessage(HttpMethod.Post, new RoleRouteBuilder().RoleId(roleId).ToString())
            {
                Content = new StringContent(JsonConvert.SerializeObject(permissionModels), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<RoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<RoleApiModel> DeletePermissionFromRole(string roleId, List<PermissionApiModel> permissionModels)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, new RoleRouteBuilder().RoleId(roleId).ToString())
            {
                Content = new StringContent(JsonConvert.SerializeObject(permissionModels), Encoding.UTF8, "application/json")
            };

            return await SendAndParseJson<RoleApiModel>(message).ConfigureAwait(false);
        }

        public async Task<UserApiModel> GetRole(string roleId)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, new RoleRouteBuilder().RoleId(roleId).ToString());
            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        public async Task<UserApiModel> GetRole(string grain, string securableItem, string roleName)
        {
            var message = new HttpRequestMessage(HttpMethod.Get,
                new RoleRouteBuilder().Grain(grain).SecurableItem(securableItem).Name(roleName).ToString());
            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
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
    }
}
namespace Catalyst.Fabric.Authorization.Client.FunctionalTests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Catalyst.Fabric.Authorization.Models;
    using Xunit;

    public class FunctionalTestFixture : IDisposable
    {
        protected string _installerAccessToken;
        protected string _funcTestClientSecret;
        protected string _authAccessToken;
        protected Guid? _roleId;
        protected FunctionalTestSettings args;
        protected UserApiModel user;
        protected HttpClient _authorizationClient;
        protected HttpClient _identityClient;

        public Uri BaseUrl { get; set; }

        public FunctionalTestFixture()
        {
            this.GetParameters();

            this._authorizationClient = new HttpClient();
            this._authorizationClient.BaseAddress = new Uri(this.args.AuthorizationBaseUrl);
            this.BaseUrl = this._authorizationClient.BaseAddress;

            this._identityClient = new HttpClient();
            this._identityClient.BaseAddress = new Uri(this.args.IdentityBaseUrl);

            var tokenForInstaller = Task.Run(async () => await this.GetAccessTokenForInstaller());
            tokenForInstaller.Wait();
            this._installerAccessToken = tokenForInstaller.Result;

            Task.Run(async () => await this.DeleteClient(FunctionalTestConstants.IdentityTestUser)).Wait();
            Task<string> client = Task.Run(async () => await this.CreateClient(FunctionalTestConstants.IdentityTestUser));
            client.Wait();
            this._funcTestClientSecret = client.Result;

            Task<string> tokenForAuthClient = Task.Run(async () => await GetAccessTokenForAuthClient());
            tokenForAuthClient.Wait();
            this._authAccessToken = tokenForAuthClient.Result;

            // comment these out if you have ran them once locally.  
            Task.Run(async () => await this.RegisterClient()).Wait();
            Task.Run(async () => await this.RegisterViewPermissions()).Wait();
            Task.Run(async () => await this.RegisterEditPermissions()).Wait();
            Task.Run(async () => await this.RegisterRole()).Wait();
            Task.Run(async () => await this.RegisterGroup()).Wait();
            Task.Run(async () => await this.AssociateGroupsToRoles()).Wait();
            Task.Run(async () => await this.AssociateUserWithGroup()).Wait();
            Task.Run(async () => await this.AssociateRolesWithPermissions()).Wait();
        }

        private void GetParameters()
        {
            this.args = new FunctionalTestSettings
            {
                IdentityBaseUrl = Environment.GetEnvironmentVariable("FABRIC_IDENTITY_URL") ?? "http://localhost:5001/",
                AuthorizationBaseUrl =
                    Environment.GetEnvironmentVariable("FABRIC_AUTH_URL") ?? "http://localhost:5004/",
                InstallerClientSecret =
                    Environment.GetEnvironmentVariable("FABRIC_INSTALLER_SECRET"), // replace with local secret when running through VSTS
                AuthClientSecret = Environment.GetEnvironmentVariable("FABRIC_AUTH_SECRET") // replace with local secret when running through VSTS
            };

            Console.WriteLine($"Test configured with: {0}={1}", (object)"FABRIC_IDENTITY_URL", (object)this.args.IdentityBaseUrl);
            Console.WriteLine($"Test configured with: {0}={1}", (object)"FABRIC_AUTH_URL", (object)this.args.AuthorizationBaseUrl);
            Console.WriteLine($"Test configured with: {0}={1}", (object)"FABRIC_INSTALLER_SECRET", (object)this.args.InstallerClientSecret);
            Console.WriteLine($"Test configured with: {0}={1}", (object)"FABRIC_AUTH_SECRET", (object)this.args.AuthClientSecret);
        }

        public async Task<string> GetAccessToken(HttpRequestMessage message)
        {
            message.RequestUri = new Uri(_identityClient.BaseAddress, "/connect/token");
            message.Method = HttpMethod.Post;

            var response = await _identityClient.SendAsync(message).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var accessToken = content.FromJson<ClientModel>().access_token;
            return accessToken;
        }

        public async Task<string> GetAccessTokenForAuthClient()
        {
            string accessToken = await this.GetAccessToken(new HttpRequestMessage
            {
                Content = new FormUrlEncodedContent(
                    FunctionalTestHelpers.GetAccessTokenKeyValuePair(
                        FunctionalTestConstants.IdentityTestUser,
                        _funcTestClientSecret,
                        "client_credentials",
                        "fabric/authorization.read fabric/authorization.write"))
            }).ConfigureAwait(false);
            return accessToken;
        }

        public async Task<string> GetAccessTokenForInstaller()
        {
            string accessToken = await this.GetAccessToken(new HttpRequestMessage
            {
                Content = new FormUrlEncodedContent(
                    FunctionalTestHelpers.GetAccessTokenKeyValuePair(
                        "fabric-installer",
                        this.args.InstallerClientSecret,
                        "client_credentials",
                        "fabric/identity.manageresources fabric/authorization.read fabric/authorization.write fabric/authorization.manageclients"))
            }).ConfigureAwait(false);
            return accessToken;
        }

        public async Task<string> GetUserAccessToken(string username, string password)
        {
            string stringToEncode = $"{FunctionalTestConstants.IdentityTestUser}:{this._funcTestClientSecret}";
            string encodedData = stringToEncode.ToBase64Encoded();
            string accessToken = await this.GetAccessToken(new HttpRequestMessage
            {
                Headers = {
                  Authorization = new AuthenticationHeaderValue(FunctionalTestConstants.Basic, encodedData)
                },
                Content = FunctionalTestHelpers.GetResourceOwnerPasswordPostBody(username, password)
            }).ConfigureAwait(false);
            return accessToken;
        }

        public async Task<string> CreateClient(string username)
        {
            string requestContent = FunctionalTestHelpers.CreateFunctionalTestClient(username);
            var response = await this._identityClient.SendAsync(new HttpRequestMessage()
            {
                RequestUri = new Uri(new Uri(this.args.IdentityBaseUrl), "/api/client"),
                Method = HttpMethod.Post,
                Headers = {
                  Authorization = new AuthenticationHeaderValue(FunctionalTestConstants.Bearer, this._installerAccessToken),
                  Accept = {
                    new MediaTypeWithQualityHeaderValue(FunctionalTestConstants.Applicationjson)
                  }
                },
                Content = new StringContent(requestContent, Encoding.UTF8, FunctionalTestConstants.Applicationjson)
            }).ConfigureAwait(false);
            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return content.FromJson<ClientModel>().clientSecret;
        }

        public async Task<string> DeleteClient(string username)
        {
            var message = new HttpRequestMessage();
            message.Headers.Authorization = new AuthenticationHeaderValue(FunctionalTestConstants.Bearer, this._installerAccessToken);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(FunctionalTestConstants.Applicationjson));
            message.RequestUri = new Uri(_identityClient.BaseAddress, $"/api/client/{username}");
            message.Method = HttpMethod.Delete;

            var response = await _identityClient.SendAsync(message).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public async Task RegisterEditPermissions()
        {
            var response = await this.PostWithJsonBody(this._authorizationClient.BaseAddress, "/Permissions", FunctionalTestHelpers.UserCanEditPermissions, this._installerAccessToken).ConfigureAwait(false);
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        }

        private async Task RegisterClient()
        {
            var response = await this.PostWithJsonBody(this._authorizationClient.BaseAddress, "/clients", FunctionalTestHelpers.AuthClientFuncTest, this._installerAccessToken).ConfigureAwait(false);
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        }

        public async Task RegisterViewPermissions()
        {
            var response = await this.PostWithJsonBody(this._authorizationClient.BaseAddress, "/Permissions", FunctionalTestHelpers.UserCanViewPermissions, this._installerAccessToken).ConfigureAwait(false);
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        }

        public async Task RegisterGroup()
        {
            var response = await this.PostWithJsonBody(this._authorizationClient.BaseAddress, "/groups", FunctionalTestHelpers.GroupHcAdmin, this._installerAccessToken).ConfigureAwait(false);
        }

        private async Task RegisterRole()
        {
            var response = await this.PostWithJsonBody(this._authorizationClient.BaseAddress, "/roles", FunctionalTestHelpers.RoleHcAdmin, this._installerAccessToken).ConfigureAwait(false);
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            string responseString = await response.Content.ReadAsStringAsync();
            RoleApiModel responseRole = responseString.FromJson<RoleApiModel>();
            this._roleId = responseRole.Id;
        }

        private async Task AssociateGroupsToRoles()
        {
            var roleRoute = $"/roles/{FunctionalTestConstants.Grain}/{FunctionalTestConstants.IdentityTestUser}/{WebUtility.UrlEncode(FunctionalTestConstants.GroupName)}";
            var response = await this.Get(this._authorizationClient.BaseAddress, roleRoute, this._installerAccessToken).ConfigureAwait(false);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            var stringResponse = await response.Content.ReadAsStringAsync();
            var roleResponse = stringResponse.FromJson<RoleApiModel[]>();
            var groupRoute = $"/groups/{WebUtility.UrlEncode(FunctionalTestConstants.GroupName)}/roles";
            var response2 = await this.PostWithJsonBody(this._authorizationClient.BaseAddress, groupRoute, string.Format("[{0}]", roleResponse[0].ToJson()), this._installerAccessToken).ConfigureAwait(false);
            Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);
        }

        private async Task AssociateUserWithGroup()
        {
            var groupRoute = $"/groups/{WebUtility.UrlEncode(FunctionalTestConstants.GroupName)}/users";
            var response = await this.PostWithJsonBody(this._authorizationClient.BaseAddress, groupRoute, string.Format("[{0}]", FunctionalTestHelpers.UserBob), this._installerAccessToken).ConfigureAwait(false);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        private async Task AssociateRolesWithPermissions()
        {
            var permissionsRoute = $"/permissions/{FunctionalTestConstants.Grain}/{FunctionalTestConstants.IdentityTestUser}";
            var response = await this.Get(this._authorizationClient.BaseAddress, permissionsRoute, this._installerAccessToken);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            var stringResponse = await response.Content.ReadAsStringAsync();
            var rolesAndPermissionsRoute = $"/roles/{this._roleId}/permissions";
            var response2 = await this.PostWithJsonBody(this._authorizationClient.BaseAddress, rolesAndPermissionsRoute, stringResponse, this._installerAccessToken).ConfigureAwait(false);
            Assert.Equal(System.Net.HttpStatusCode.Created, response2.StatusCode);
        }

        private async Task<HttpResponseMessage> PostWithJsonBody(Uri baseUrl, string relativePath, string content, string accessToken)
        {
            return await this._authorizationClient.SendAsync(new HttpRequestMessage()
            {
                RequestUri = new Uri(baseUrl, relativePath),
                Method = HttpMethod.Post,
                Headers = {
                  Authorization = new AuthenticationHeaderValue(FunctionalTestConstants.Bearer, accessToken),
                  Accept = {
                    new MediaTypeWithQualityHeaderValue(FunctionalTestConstants.Applicationjson)
                  }
                },
                Content = (HttpContent)new StringContent(content, Encoding.UTF8, FunctionalTestConstants.Applicationjson)
            }).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> Get(Uri baseUrl, string relativePath, string accessToken)
        {
            return await this._authorizationClient.SendAsync(new HttpRequestMessage()
            {
                RequestUri = new Uri(baseUrl, relativePath),
                Method = HttpMethod.Get,
                Headers = {
                  Authorization = new AuthenticationHeaderValue(FunctionalTestConstants.Bearer, accessToken),
                  Accept = {
                    new MediaTypeWithQualityHeaderValue(FunctionalTestConstants.Applicationjson)
                  }
                }
            }).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _authorizationClient.Dispose();
            _identityClient.Dispose();
        }
    }

    [CollectionDefinition(FunctionalTestConstants.FunctionTestTitle)]
    public class FunctionalTestCollection : ICollectionFixture<FunctionalTestFixture>
    {
    }
}

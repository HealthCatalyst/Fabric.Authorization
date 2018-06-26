namespace Fabric.Authorization.Client.FunctionalTests
{
    using System.Collections.Generic;
    using System.Net.Http;

    public static class FunctionalTestHelpers
    {
        public static string UserCanViewPermissions
        {
            get
            {
                return new
                {
                    Grain = FunctionalTestConstants.Grain,
                    SecurableItem = FunctionalTestConstants.IdentityTestUser,
                    Name = "userCanView"
                }.ToJson();
            }
        }

        public static string UserCanEditPermissions
        {
            get
            {
                return new
                {
                    Grain = FunctionalTestConstants.Grain,
                    SecurableItem = FunctionalTestConstants.IdentityTestUser,
                    Name = "userCanEdit"
                }.ToJson();
            }
        }

        public static string UserBob
        {
            get
            {
                return new
                {
                    subjectId = "88421113",
                    identityProvider = "test"
                }.ToJson();
            }
        }

        public static string RoleHcAdmin
        {
            get
            {
                return new
                {
                    Grain = FunctionalTestConstants.Grain,
                    SecurableItem = FunctionalTestConstants.IdentityTestUser,
                    Name = FunctionalTestConstants.GroupName
                }.ToJson();
            }
        }

        public static string GroupHcAdmin
        {
            get
            {
                return new
                {
                    groupName = FunctionalTestConstants.GroupName,
                    groupSource = "custom"
                }.ToJson();
            }
        }

        public static string AuthClientFuncTest
        {
            get
            {
                return new
                {
                    id = FunctionalTestConstants.IdentityTestUser,
                    name = "Functional Test",
                    topLevelSecurableItem = new { name = FunctionalTestConstants.IdentityTestUser }
                }.ToJson();
            }
        }

        public static string CreateFunctionalTestClient(string username)
        {
            return new
            {
                clientId = username,
                clientName = "Functional Test Client",
                requireConsent = "false",
                allowedGrantTypes = new string[2]
                  {
                      "client_credentials",
                      "password"
                  },
                    allowedScopes = new string[5]
                  {
                      "fabric/identity.manageresources",
                      "fabric/authorization.read",
                      "fabric/authorization.write",
                      "openid",
                      "profile"
                  }
            }.ToJson();
        }

        public static FormUrlEncodedContent GetResourceOwnerPasswordPostBody(string username, string password)
        {
            return new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)new KeyValuePair<string, string>[3]
            {
                new KeyValuePair<string, string>("grant_type", nameof (password)),
                new KeyValuePair<string, string>(nameof (username), username),
                new KeyValuePair<string, string>(nameof (password), password)
            });
        }

        public static IEnumerable<KeyValuePair<string, string>> GetAccessTokenKeyValuePair(string clientId, string clientSecret, string grantType, string scope)
        {
            return (IEnumerable<KeyValuePair<string, string>>)new KeyValuePair<string, string>[4]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("grant_type", grantType),
                new KeyValuePair<string, string>(nameof (scope), scope)
            };
        }
    }
}

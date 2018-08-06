using Fabric.Authorization.API.Configuration;
using Nancy;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.API.Modules
{
    public class RootModule : NancyModule
    {
        private readonly IAppConfiguration _appConfiguration;
        public RootModule(IAppConfiguration appConfiguration): base("/v1")
        {
            _appConfiguration = appConfiguration;
            Get("/", _ => Redirect(), null, "Redirect");
            Get($"/{AccessControl.Path}", _ => Redirect(), null, "Redirect");
        }

        private dynamic Redirect()
        {
            var redirectUri = $"{_appConfiguration.ApplicationEndpoint.EnsureTrailingSlash()}{AccessControl.Path}/{AccessControl.Index}";
            return Response.AsRedirect(redirectUri);
        }
    }
}
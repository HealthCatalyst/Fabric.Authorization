using Nancy;
using Fabric.Authorization.API.Constants;

namespace Fabric.Authorization.API.Modules
{
    public class RootModule : NancyModule
    {
        public RootModule(): base("/v1")
        {
            Get("/", _ => Redirect(), null, "Redirect");
        }

        private dynamic Redirect()
        {
            return Response.AsRedirect($"/{AccessControl.Path}/index.html");
        }
    }
}
using Nancy;

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
            return Response.AsRedirect("/client/index.html");
        }
    }
}
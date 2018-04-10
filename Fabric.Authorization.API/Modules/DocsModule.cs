using Nancy;
using Nancy.Swagger.Services;

namespace Fabric.Authorization.API.Modules
{
    public class DocsModule : NancyModule
    {
        public DocsModule(ISwaggerMetadataProvider converter) : base("/v1/swagger/ui")
        {
            Get("/index", _ => GetSwaggerUrl());
            Get("/swagger.json", _ => converter.GetSwaggerJson(Context).ToJson());
        }

        private Response GetSwaggerUrl()
        {
            return Response.AsRedirect(
                $"index.html?url=swagger.json");
        }
    }
}
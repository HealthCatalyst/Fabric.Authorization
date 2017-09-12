using Nancy.Swagger;
using Nancy.Swagger.Services;

namespace Fabric.Authorization.API.Modules
{
    public class IdentitySearchMetadataModule : BaseMetadataModule
    {
        public IdentitySearchMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog)
            : base(modelCatalog, tagCatalog)
        {
        }
    }
}
using Nancy.Swagger;
using Nancy.Swagger.Services;

namespace Fabric.Authorization.API.Models.Documentation
{
    /*public class ClientApiModelDataProvider : ISwaggerModelDataProvider
    {
        public SwaggerModelData GetModelData()
        {
            return SwaggerModelData.ForType<ClientApiModel>(with =>
            {
                with.Description("Client model abc");

                with.Property(x => x.Id)
                    .Description("Unique client ID - required for all operations except for POST")
                    .Required(true)
                    .Default("abc");

                with.Property(x => x.CreatedDateTimeUtc)
                    .Required(false);
            });
        }
    }*/
}
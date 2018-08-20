using Catalyst.Fabric.Authorization.Models;
using Nancy;
using Nancy.Swagger;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;

namespace Fabric.Authorization.API.Modules
{
    public class SecurableItemsMetadataModule : BaseMetadataModule
    {
        private readonly Parameter _securableItemIdParameter = new Parameter
        {
            Name = "securableItemId",
            Description = "The id of the securable item",
            Required = true,
            Type = "string",
            In = ParameterIn.Path
        };

        private readonly Tag _securableItemsTag = new Tag
        {
            Name = "Securable Item",
            Description = "Operations for managing Securable Items"
        };

        public SecurableItemsMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog)
            : base(modelCatalog, tagCatalog)
        {
            RouteDescriber.DescribeRoute(
                "GetSecurableItem",
                "",
                "Gets the top level securable item by client id",
                new[]
                {
                    new HttpResponseMetadata<SecurableItemApiModel>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "OK"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "The client was not found by client id"
                    }
                },
                new[]
                {
                    _securableItemsTag
                }).SecurityRequirement(OAuth2ReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "GetSecurableItemById",
                "",
                "Gets a securable item by client id and securable item id",
                new[]
                {
                    new HttpResponseMetadata<SecurableItemApiModel>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "OK"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "The client was not found by client id or the securable item was not found"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.BadRequest,
                        Message = "The securable item id must be a guid"
                    }
                },
                new[]
                {
                    _securableItemIdParameter
                },
                new[]
                {
                    _securableItemsTag
                }).SecurityRequirement(OAuth2ReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "AddSecurableItem",
                "",
                "Add a new securable item to the specified securable item hierarchy",
                new[]
                {
                    new HttpResponseMetadata<SecurableItemApiModel>
                    {
                        Code = (int) HttpStatusCode.Created,
                        Message = "Created"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.BadRequest,
                        Message =
                            "The securable item id is not a guid or the securable item failed validation"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message =
                            "The client was not found by client id or the specified securable item by id was not found"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Conflict,
                        Message = "The securable item with the specified id already exists"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.UnsupportedMediaType,
                        Message = "Content-Type header was not included in request"
                    }
                },
                new[]
                {
                    _securableItemIdParameter,
                    new BodyParameter<SecurableItemApiModel>(modelCatalog)
                    {
                        Name = "Securable Item",
                        Description = "The securable item to add"
                    }
                },
                new[]
                {
                    _securableItemsTag
                }).SecurityRequirement(OAuth2WriteScopeBuilder);
        }
    }
}
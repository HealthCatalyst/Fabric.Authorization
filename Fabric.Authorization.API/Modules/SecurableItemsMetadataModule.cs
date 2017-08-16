using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Nancy;
using Nancy.Swagger;
using Nancy.Swagger.Modules;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;

namespace Fabric.Authorization.API.Modules
{
    public class SecurableItemsMetadataModule : SwaggerMetadataModule
    {
        private readonly Tag _securableItemsTag = new Tag
        {
            Name = "Securable Item",
            Description = "Operations for managing Securable Items"
        };

        private readonly Parameter _securableItemIdParameter = new Parameter
        {
            Name = "securableItemId",
            Description = "The id of the securable item",
            Required = true,
            Type = "Guid",
            In = ParameterIn.Path
        };

        public SecurableItemsMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog) 
            : base(modelCatalog, tagCatalog)
        {
            modelCatalog.AddModels(
                typeof(SecurableItemApiModel)
                );

            RouteDescriber.DescribeRoute(
                "GetSecurableItem",
                "",
                "Gets the top level securable item by client id",
                new[]
                {
                    new HttpResponseMetadata<SecurableItemApiModel>()
                    {
                        Code = (int)HttpStatusCode.OK,
                        Message = "OK"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Message = "The client was not found by client id"
                    }
                },
                new[]
                {
                    _securableItemsTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "GetSecurableItemById",
                "",
                "Gets a securable item by client id and securable item id",
                new[]
                {
                    new HttpResponseMetadata<SecurableItemApiModel>
                    {
                        Code = (int)HttpStatusCode.OK,
                        Message = "OK"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Message = "The client was not found by client id or the securable item was not found"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.BadRequest,
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
                });

            RouteDescriber.DescribeRouteWithParams(
                "AddSecurableItem",
                "",
                "Add a new securable item",
                new[]
                {
                    new HttpResponseMetadata<SecurableItemApiModel>
                    {
                        Code = (int)HttpStatusCode.Created,
                        Message = "Created"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "The securable item id is not a guid, the securable item failed validation, or it already exists"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Message = "The client was not found by client id"
                    }
                },
                new[]
                {
                    new BodyParameter<SecurableItemApiModel>(modelCatalog)
                    {
                        Name = "Securable Item",
                        Description = "The securable item to add"
                    },
                },
                new[]
                {
                    _securableItemsTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "AddSecurableItemById",
                "",
                "Add a new securable item by the specified securable item id",
                new[]
                {
                    new HttpResponseMetadata<SecurableItemApiModel>
                    {
                        Code = (int)HttpStatusCode.Created,
                        Message = "Created"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "The securable item id is not a guid, the securable item failed validation, or it already exists"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Message = "The client was not found by client id or the specified securable item by id was not found"
                    }
                },
                new[]
                {
                    _securableItemIdParameter,
                    new BodyParameter<SecurableItemApiModel>(modelCatalog)
                    {
                        Name = "Securable Item",
                        Description = "The securable item to add"
                    },
                },
                new[]
                {
                    _securableItemsTag
                });
        }
    }
}

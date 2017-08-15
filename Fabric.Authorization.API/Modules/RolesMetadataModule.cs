using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nancy;
using Nancy.Swagger;
using Nancy.Swagger.Modules;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;

namespace Fabric.Authorization.API.Modules
{
    public class RolesMetadataModule : SwaggerMetadataModule
    {
        public RolesMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog) 
            : base(modelCatalog, tagCatalog)
        {
            RouteDescriber.DescribeRouteWithParams(
                "GetRolesBySecurableItem",
                "",
                "Get roles associated with a securable item",
                new []
                {
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.OK,
                        Message = "OK"
                    },
                },
                new []
                {
                    new Parameter
                    {
                        Name = "grain",
                        Description = "The top level grain to return permissions for",
                        Required = true,
                        Type = "string",
                        In = ParameterIn.Path,
                    },
                    new Parameter
                    {
                        Name = "securableItem",
                        Description = "The specific securableItem within the grain to return permissions for",
                        Required = true,
                        Type = "string",
                        In = ParameterIn.Path
                    }
                },
                new []
                {
                    new Tag{Name = "Roles", Description = "Operations for managing roles"}
                });
        }
    }
}

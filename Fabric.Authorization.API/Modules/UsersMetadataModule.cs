using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using Nancy.Swagger.Modules;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;

namespace Fabric.Authorization.API.Modules
{
    public class UsersMetadataModule : SwaggerMetadataModule
    {
        public UsersMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog) 
            : base(modelCatalog, tagCatalog)
        {
            RouteDescriber.DescribeRoute<UserPermissionsApiModel>("GetUserPermissions", "", "Gets permissions for a user", new[]
            {
                new HttpResponseMetadata {Code = 200, Message = "OK"}
            });
        }
    }
}

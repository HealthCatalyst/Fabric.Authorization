using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Swagger;
using Nancy;
using Nancy.Swagger;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;
using System.Collections.Generic;

namespace Fabric.Authorization.API.Modules
{
    public class PermissionsMetadataModule : BaseMetadataModule
    {
        private readonly Tag _permissionsTag = new Tag { Name = "Permissions", Description = "Operations for managing permissions"};
        
        private readonly Parameter _permissionNameParameter = new Parameter
        {
            Name = "permissionName",
            Description = "The name of the permission",
            Required = true,
            Type = "string",
            In = ParameterIn.Path
        };

        private readonly Parameter _permissionIdParameter = new Parameter
        {
            Name = "permissionId",
            Description = "The id of the permission",
            Required = true,
            Type = "string",
            In = ParameterIn.Path
        };

        public PermissionsMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog) 
            : base(modelCatalog, tagCatalog)
        {
            RouteDescriber.DescribeRouteWithParams(
                 "GetPermissionsForSecurableItem",
                 "",
                 "Get permissions for a particular grain and securable item",
                 new[]
                 {
                    new HttpResponseMetadata<IEnumerable<PermissionApiModel>>
                    {
                        Code = (int)HttpStatusCode.OK,
                        Message = "OK"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    }
                 },
                 new[]
                 {
                    Parameters.GrainParameter,
                    Parameters.SecurableItemParameter
                 },
                 new[]
                 {
                    _permissionsTag
                 }).SecurityRequirement(OAuth2ReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "GetPermissionByName",
                "",
                "Get permissions for a particular grain, securable item, and permission name",
                new[]
                {
                    new HttpResponseMetadata<IEnumerable<PermissionApiModel>>
                    {
                        Code = (int)HttpStatusCode.OK,
                        Message = "OK"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    }
                },
                new[]
                {
                    Parameters.GrainParameter,
                    Parameters.SecurableItemParameter,
                    _permissionNameParameter
                },
                new[]
                {
                    _permissionsTag
                }).SecurityRequirement(OAuth2ReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "GetPermissionById",
                "",
                "Get a permission by permission id",
                new[]
                {
                    new HttpResponseMetadata<PermissionApiModel>
                    {
                        Code = (int)HttpStatusCode.OK,
                        Message = "Permission was found"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "Permission id must be a Guid"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Message = "Permission with the specified id was not found"
                    }
                },
                new[]
                {
                    _permissionIdParameter
                },
                new[]
                {
                    _permissionsTag
                }).SecurityRequirement(OAuth2ReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "AddPermission",
                "",
                "Adds a new permissions",
                new[]
                {
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Created,
                        Message = "Permission was created"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "Permission data in body is invalid"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Conflict,
                        Message = "Permission with the specified id already exists"
                    }
                },
                new[]
                {
                    new BodyParameter<PermissionApiModel>(modelCatalog)
                    {
                        Name = "Permission",
                        Description = "The permission to add"
                    }
                },
                new[]
                {
                    _permissionsTag
                }).SecurityRequirement(OAuth2WriteScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "DeletePermission",
                "",
                "Deletes a permission",
                new[]
                {
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NoContent,
                        Message = "Permission with the specified id was deleted"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "Permission id must be a guid"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Message = "Permission with specified id was not found"
                    }
                },
                new[]
                {
                    _permissionIdParameter
                },
                new[]
                {
                    _permissionsTag
                }).SecurityRequirement(OAuth2WriteScopeBuilder);
        }
    }
}

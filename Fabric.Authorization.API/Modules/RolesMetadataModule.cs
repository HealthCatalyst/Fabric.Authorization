using Fabric.Authorization.API.Models;
using Nancy;
using Nancy.Swagger;
using Nancy.Swagger.Modules;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;
using System;
using System.Collections.Generic;
using Fabric.Authorization.API.Swagger;

namespace Fabric.Authorization.API.Modules
{
    public class RolesMetadataModule : SwaggerMetadataModule
    {
        private readonly Tag _rolesTag = new Tag {Name = "Roles", Description = "Operations for managing roles"};

        private readonly Parameter _roleNameParameter = new Parameter
        {
            Name = "roleName",
            Description = "The name of the role",
            Required = true,
            Type = "string",
            In = ParameterIn.Path
        };

        private readonly Parameter _roleIdParameter = new Parameter
        {
            Name = "roleId",
            Description = "The id of the role",
            Required = true,
            Type = "string",
            In = ParameterIn.Path
        };

        public RolesMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog)
            : base(modelCatalog, tagCatalog)
        {
            modelCatalog.AddModels(
                typeof(Guid),
                typeof(Guid?),
                typeof(PermissionApiModel));

            RouteDescriber.DescribeRouteWithParams(
                "GetRolesBySecurableItem",
                "",
                "Get roles associated with a securable item",
                new[]
                {
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.OK,
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
                    _rolesTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "GetRoleByName",
                "",
                "Get a role by role name",
                new[]
                {
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "Role with specified name was found"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    }
                },
                new[]
                {
                    Parameters.GrainParameter,
                    Parameters.SecurableItemParameter,
                    _roleNameParameter
                },
                new[]
                {
                    _rolesTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "AddRole",
                "",
                "Add a new role",
                new[]
                {
                    new HttpResponseMetadata<RoleApiModel>
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
                        Message = "Role with specified id already exists or Role object in body failed validation"
                    }
                },
                new[]
                {
                    new BodyParameter<RoleApiModel>(modelCatalog)
                    {
                        Name = "Role",
                        Description = "The role to add"
                    }
                },
                new[]
                {
                    _rolesTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "DeleteRole",
                "",
                "Deletes a role",
                new[]
                {
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NoContent,
                        Message = "Role with the specified id was deleted"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "Invalid roled id provided"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Message = "Role with specified id was not found"
                    }
                },
                new[]
                {
                    _roleIdParameter
                },
                new[]
                {
                    _rolesTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "AddPermissionsToRole",
                "",
                "Add permissions to an existing role",
                new[]
                {
                    new HttpResponseMetadata<RoleApiModel>
                    {
                        Code = (int)HttpStatusCode.Created,
                        Message = "Permission added to role"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "Invalid role id, no permissions specified to add, or incompatible permission provided"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Message = "Role not found or permission not found"
                    }
                },
                new[]
                {
                    _roleIdParameter,
                    new BodyParameter<IEnumerable<PermissionApiModel>>(modelCatalog)
                    {
                        Name = "List of permissions",
                        Description = "The list of permissions to add to the role"
                    }
                },
                new[]
                {
                    _rolesTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "DeletePermissionsFromRole",
                "",
                "Delete permissions from an existing role",
                new[]
                {
                    new HttpResponseMetadata<RoleApiModel>
                    {
                        Code = (int)HttpStatusCode.Created,
                        Message = "Permission removed from role"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "Invalid role id or no permissions specified to delete from role"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Message = "Role not found or permission not found"
                    }
                },
                new[]
                {
                    _roleIdParameter,
                    new BodyParameter<IEnumerable<PermissionApiModel>>(modelCatalog)
                    {
                        Name = "List of permissions",
                        Description = "The list of permissions to add to the role"
                    }
                },
                new[]
                {
                    _rolesTag
                });
        }
    }
}

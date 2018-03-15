using System.Collections.Generic;
using Fabric.Authorization.API.Models;
using Nancy;
using Nancy.Swagger;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;

namespace Fabric.Authorization.API.Modules
{
    public class UsersMetadataModule : BaseMetadataModule
    {
        private readonly Parameter _subjectIdParameter = new Parameter
        {
            Name = "subjectId",
            Description = "Subject ID (from external identity provider)",
            In = ParameterIn.Path,
            Required = true,
            Type = "string"
        };

        private readonly Parameter _identityProviderParameter = new Parameter
        {
            Name = "identityProvider",
            Description = "External identity provider name",
            In = ParameterIn.Path,
            Required = true,
            Type = "string"
        };

        private readonly Tag _usersTag =
            new Tag {Name = "Users", Description = "Operations related to user permissions"};

        public UsersMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog)
            : base(modelCatalog, tagCatalog)
        {
            ModelCatalog.AddModels(typeof(PermissionAction));
            ModelCatalog.AddModels(typeof(PermissionRoleApiModel));
            ModelCatalog.AddModels(typeof(ResolvedPermissionApiModel));
            ModelCatalog.AddModels(typeof(UserApiModel));
            ModelCatalog.AddModels(typeof(List<RoleApiModel>));
            ModelCatalog.AddModels(typeof(PermissionRequestContext));

            RouteDescriber.DescribeRouteWithParams(
                "AddUser",
                "",
                "Adds a new user.",
                new []
                {
                    new HttpResponseMetadata<UserApiModel>
                    {
                        Code = (int) HttpStatusCode.Created,
                        Message = "Created"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "User does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.BadRequest,
                        Message = "User object in body failed validation"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.Conflict,
                        Message = "User with specified IdentityProvider and Subject already exists"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.UnsupportedMediaType,
                        Message = "Content-Type header was not included in request"
                    }
                },
                new[]
                {
                    new BodyParameter<UserApiModel>(modelCatalog)
                    {
                        Name = "User",
                        Description = "The user to add"
                    }
                },
                new []
                {
                    _usersTag
                }).SecurityRequirement(OAuth2WriteScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "AddRolesToUser",
                "",
                "Adds roles to an existing user.",
                new[]
                {
                    new HttpResponseMetadata<UserApiModel>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "Roles added."
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "User does not have access to add the specified roles."
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.BadRequest,
                        Message = "List of roles in body failed validation"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "Specified user does not exist"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.UnsupportedMediaType,
                        Message = "Content-Type header was not included in request"
                    }
                },
                new[]
                {
                    new BodyParameter<List<RoleApiModel>>(modelCatalog)
                    {
                        Name = "Roles",
                        Description = "The roles to add"
                    }
                },
                new[]
                {
                    _usersTag
                }).SecurityRequirement(OAuth2WriteScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "DeleteRolesFromUser",
                "",
                "Deletes roles from existing user.",
                new[]
                {
                    new HttpResponseMetadata<UserApiModel>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "Roles deleted."
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "User does not have access to add the specified roles."
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.BadRequest,
                        Message = "List of roles in body failed validation"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "Specified user does not exist"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.UnsupportedMediaType,
                        Message = "Content-Type header was not included in request"
                    }
                },
                new[]
                {
                    new BodyParameter<List<RoleApiModel>>(modelCatalog)
                    {
                        Name = "Roles",
                        Description = "The roles to delete."
                    }
                },
                new[]
                {
                    _usersTag
                }).SecurityRequirement(OAuth2WriteScopeBuilder);

            RouteDescriber.DescribeRoute(
                "GetCurrentUserPermissions",
                "",
                "Gets permissions for currently authenticated user",
                new[]
                {
                    new HttpResponseMetadata<UserPermissionsApiModel> {Code = (int) HttpStatusCode.OK, Message = "OK"},
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    }
                },
                new[]
                {
                    _usersTag
                }).SecurityRequirement(OAuth2ReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "GetUserPermissions",
                "",
                "Gets permissions for specified user. Note this will only retrieve 1) granular permissions and 2) permissions under roles mapped to Custom groups.",
                new[]
                {
                    new HttpResponseMetadata<List<ResolvedPermissionApiModel>> {Code = (int) HttpStatusCode.OK, Message = "OK"},
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    }
                },
                new[]
                {
                    _identityProviderParameter,
                    _subjectIdParameter
                },
                new[]
                {
                    _usersTag
                }).SecurityRequirement(OAuth2ReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "AddGranularPermissions",
                "",
                "Adds granular permissions for a user, either to allow or deny",
                new[]
                {
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.NoContent,
                        Message = "Granular permissions were added"
                    },                  
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.BadRequest,
                        Message = "No permissions to add included in request."
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Conflict,
                        Message = "The permissions specified already exist either as duplicates or with a different permission action than the one specified or a permission is in the request as both allow and deny"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.UnsupportedMediaType,
                        Message = "Content-Type header was not included in request"
                    }
                },
                new[]
                {
                    _identityProviderParameter,
                    _subjectIdParameter,
                    new BodyParameter<List<PermissionApiModel>>(modelCatalog)
                    {
                        Name = "GranularPermissions",
                        Description = "The permissions to add for the user."
                    }
                },
                new[]
                {
                    _usersTag
                }).SecurityRequirement(OAuth2ManageClientsScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "DeleteGranularPermissions",
                "",
                "Deletes granular permissions for a user",
                new[]
                {
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.NoContent,
                        Message = "The permissions were deleted"
                    },                    
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.BadRequest,
                        Message = "No permissions were specified or the permissions specified do not exist or already exist with a different permission action."
                    }
                },
                new[]
                {
                    _identityProviderParameter,
                    _subjectIdParameter,
                    new BodyParameter<List<PermissionApiModel>>(modelCatalog)
                    {
                        Name = "GranularPermissions",
                        Description = "The permissions to delete from the user."
                    }
                },
                new[]
                {
                    _usersTag
                }).SecurityRequirement(OAuth2ManageClientsScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "GetUserGroups",
                "",
                "Gets custom groups for a user",
                new[]
                {
                    new HttpResponseMetadata<IEnumerable<GroupUserApiModel>>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "List of GroupUserApiModel entities representing groups in which the user belongs"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "User was not found"
                    }
                },
                new[]
                {
                    _subjectIdParameter
                },
                new[]
                {
                    _usersTag
                }).SecurityRequirement(OAuth2ReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "GetUserRoles",
                "",
                "Gets the roles associated with a user",
                new[]
                {
                    new HttpResponseMetadata<List<RoleApiModel>>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "List of roles representing the roles this user has been directly associated to."
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "User was not found"
                    }
                },
                new[]
                {
                    _identityProviderParameter,
                    _subjectIdParameter
                },
                new[]
                {
                    _usersTag
                }).SecurityRequirement(OAuth2ReadScopeBuilder);
        }
    }
}
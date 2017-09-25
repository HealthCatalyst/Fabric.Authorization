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

        private readonly Parameter _userIdParameter = new Parameter
        {
            Name = "userId",
            Description = "UserId to use for the request",
            In = ParameterIn.Path,
            Required = true,
            Type = "integer"
        };

        private readonly Tag _usersTag =
            new Tag {Name = "Users", Description = "Operations related to user permissions"};

        public UsersMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog)
            : base(modelCatalog, tagCatalog)
        {
            RouteDescriber.DescribeRoute(
                "GetUserPermissions",
                "",
                "Gets permissions for a user",
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
                "AddPermissions",
                "",
                "Adds granular permissions for a user",
                new[]
                {
                    new HttpResponseMetadata {Code = (int) HttpStatusCode.NoContent},
                    new HttpResponseMetadata<Error> {Code = (int) HttpStatusCode.BadRequest, Message = "Bad Request"},
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    }
                },
                new[]
                {
                    _userIdParameter,
                    new BodyParameter<GranularPermissionApiModel>(modelCatalog)
                    {
                        Name = "GranularPermissions",
                        Description = "The permissions to explicitly allow for the user."
                    }
                },
                new[]
                {
                    _usersTag
                }).SecurityRequirement(OAuth2ManageClientsScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "AddDeniedPermissions",
                "",
                "Adds denied permissions for a user",
                new[]
                {
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.NoContent
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.BadRequest,
                        Message = "Bad Request"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    }
                },
                new[]
                {
                    _userIdParameter,
                    new BodyParameter<GranularPermissionApiModel>(modelCatalog)
                    {
                        Name = "GranularPermissions",
                        Description = "The permissions to explicitly deny for the user."
                    }
                },
                new[]
                {
                    _usersTag
                }).SecurityRequirement(OAuth2ReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "GetUserGroups",
                "",
                "Gets custom groups for a user",
                new[]
                {
                    new HttpResponseMetadata<IEnumerable<string>>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "List of strings representing group names in which the user belongs"
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
        }
    }
}
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
                "AddGranularPermissions",
                "",
                "Adds granular permissions for a user, either to allow or deny",
                new[]
                {
                    new HttpResponseMetadata {Code = (int) HttpStatusCode.NoContent},                  
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
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
                    new HttpResponseMetadata {Code = (int) HttpStatusCode.NoContent},                    
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
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
using Fabric.Authorization.API.Models;
using Nancy;
using Nancy.Swagger;
using Nancy.Swagger.Modules;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;

namespace Fabric.Authorization.API.Modules
{
    public class UsersMetadataModule : SwaggerMetadataModule
    {
        private readonly Tag _usersTag = new Tag {Name = "Users", Description = "Operations related to user permissions"};
        private readonly Parameter _userIdParameter = new Parameter
        {
            Name = "userId",
            Description = "UserId to use for the request",
            In = ParameterIn.Path,
            Required = true,            
            Type = "integer"
        };

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
                });

            RouteDescriber.DescribeRouteWithParams(
                "AddPermissions", 
                "", 
                "Adds granular permissions for a user", 
                new[]
                {
                    new HttpResponseMetadata {Code = (int) HttpStatusCode.NoContent},
                    new HttpResponseMetadata {Code = (int) HttpStatusCode.BadRequest, Message = "Bad Request"},
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    }
                },
                new[]
                {
                    _userIdParameter
                },
                new[]
                {
                    _usersTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "AddDeniedPermissions",
                "",
                "Adds denied permissions for a user",
                new[]
                {
                    new HttpResponseMetadata {Code = (int) HttpStatusCode.NoContent},
                    new HttpResponseMetadata {Code = (int) HttpStatusCode.BadRequest, Message = "Bad Request"},
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    }
                },
                new[]
                {
                    _userIdParameter
                },
                new[]
                {
                    _usersTag
                });

        }
    }
}

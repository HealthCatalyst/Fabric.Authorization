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
    public class GroupsMetadataModule : SwaggerMetadataModule
    {
        private readonly Tag _groupsTag = new Tag { Name = "Groups", Description = "Operations for managing groups" };

        private readonly Parameter _groupNameParameter = new Parameter
        {
            Name = "groupName",
            Description = "The name of the group",
            Required = true,
            Type = "string",
            In = ParameterIn.Path
        };

        public GroupsMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog) 
            : base(modelCatalog, tagCatalog)
        {
            modelCatalog.AddModels(
                typeof(GroupRoleApiModel));

           RouteDescriber.DescribeRouteWithParams(
               "AddGroup",
               "",
               "Adds a new group",
               new []
               {
                   new HttpResponseMetadata<GroupRoleApiModel>
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
                       Message = "Group already exists"
                   }
               },
               new []
               {
                   new BodyParameter<GroupRoleApiModel>(modelCatalog)
                   {
                        Name = "Group",
                        Description = "The group to add"
                   }
               },
               new []
               {
                   _groupsTag
               });

            RouteDescriber.DescribeRouteWithParams(
                "UpdateGroups",
                "",
                "Updates a list of groups",
                new[]
                {
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NoContent,
                        Message = "Groups updated"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "Group already exists"
                    }
                },
                new[]
                {
                    new BodyParameter<IEnumerable<GroupRoleApiModel>>(modelCatalog)
                    {
                        Name = "Group",
                        Description = "The groups to update"
                    }
                },
                new[]
                {
                    _groupsTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "GetGroup",
                "",
                "Gets a group by name",
                new []
                {
                    new HttpResponseMetadata<GroupRoleApiModel>
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
                        Message = "Group with specified name was not found"
                    }
                },
                new[]
                {
                    _groupNameParameter
                },
                new []
                {
                    _groupsTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "GetRolesFromGroup",
                "",
                "Gets roles for a group by group name",
                new[]
                {
                    new HttpResponseMetadata<GroupRoleApiModel>
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
                        Message = "Group with specified name was not found"
                    }
                },
                new[]
                {
                    _groupNameParameter
                },
                new[]
                {
                    _groupsTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "AddRoleToGroup",
                "",
                "Adds a role to a group",
                new[]
                {
                    new HttpResponseMetadata<GroupRoleApiModel>
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
                        Message = "Group already exists"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Message = "Group with specified name was not found or the role was not found"
                    }
                },
                new[]
                {
                    _groupNameParameter,
                    new BodyParameter<RoleApiModel>(modelCatalog)
                    {
                        Name = "Role",
                        Description = "The role to add to the group"
                    }
                },
                new[]
                {
                    _groupsTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "DeleteGroup",
                "",
                "Deletes a group",
                new []
                {
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NoContent,
                        Message = "Group deleted"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.NotFound,
                        Message = "Group with specified name was not found"
                    }
                },
                new []
                {
                    _groupNameParameter
                },
                new []
                {
                    _groupsTag
                });

            RouteDescriber.DescribeRouteWithParams(
                "DeleteRoleFromGroup",
                "",
                "Deletes a role from a group",
                new[]
                {
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.OK,
                        Message = "Role deleted from group"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int)HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata
                    {
                    Code = (int)HttpStatusCode.NotFound,
                    Message = "Group with specified name was not found or the role was not found"
                    }
                },
                new[]
                {
                    _groupNameParameter,
                    new BodyParameter<RoleApiModel>(modelCatalog)
                    {
                        Name = "Role",
                        Description = "The role to delete from the group"
                    }
                },
                new[]
                {
                    _groupsTag
                });
        }
    }
}

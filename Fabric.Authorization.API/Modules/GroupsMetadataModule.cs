using System.Collections.Generic;
using Fabric.Authorization.API.Models;
using Nancy;
using Nancy.Swagger;
using Nancy.Swagger.Services;
using Nancy.Swagger.Services.RouteUtils;
using Swagger.ObjectModel;

namespace Fabric.Authorization.API.Modules
{
    public class GroupsMetadataModule : BaseMetadataModule
    {
        private readonly Parameter _grainParameter = new Parameter
        {
            Name = "grain",
            Description = "grain",
            Required = false,
            Type = "string",
            In = ParameterIn.Query
        };

        private readonly Parameter _groupNameParameter = new Parameter
        {
            Name = "groupName",
            Description = "The name of the group",
            Required = true,
            Type = "string",
            In = ParameterIn.Path
        };

        private readonly Tag _groupsTag = new Tag {Name = "Groups", Description = "Operations for managing groups"};

        private readonly Parameter _roleIdParameter = new Parameter
        {
            Name = "Id",
            Description = "Role ID (GUID)",
            Type = "string",
            Required = true,
            In = ParameterIn.Body
        };

        private readonly Parameter _securableItemParameter = new Parameter
        {
            Name = "securableItem",
            Description = "securable item",
            Required = false,
            Type = "string",
            In = ParameterIn.Query
        };

        private readonly Parameter _subjectIdParameter = new Parameter
        {
            Name = "subjectId",
            Description = "Subject ID of the user",
            Type = "string",
            Required = true,
            In = ParameterIn.Body
        };

        public GroupsMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog)
            : base(modelCatalog, tagCatalog)
        {
            modelCatalog.AddModels(
                typeof(GroupRoleApiModel),
                typeof(GroupUserApiModel),
                typeof(UserApiModel));

            RouteDescriber.DescribeRouteWithParams(
                "AddGroup",
                "GroupSource can be either \"Custom\" for creating custom groups in Fabric or the displayName of the 3rd party identity provider if the group is from an external Idp. If groupSource is empty, it will be defaulted to the group source defined in the appsettings.json",
                "Adds a new group",
                new List<HttpResponseMetadata>
                {
                    new HttpResponseMetadata<GroupRoleApiModel>
                    {
                        Code = (int) HttpStatusCode.Created,
                        Message = "Created"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.BadRequest,
                        Message = "Group already exists"
                    }
                },
                new[]
                {
                    new BodyParameter<GroupRoleApiModel>(modelCatalog)
                    {
                        Name = "Group",
                        Description = "The group to add"
                    }
                },
                new[]
                {
                    _groupsTag
                }).SecurityRequirement(OAuth2ReadWriteScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "UpdateGroups",
                "",
                "Updates a list of groups, useful for syncing 3rd party ID Provider groups with Fabric.Authorization groups.",
                new List<HttpResponseMetadata>
                {
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.NoContent,
                        Message = "Groups updated"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.BadRequest,
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
                }).SecurityRequirement(OAuth2WriteScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "GetGroup",
                "",
                "Gets a group by name",
                new List<HttpResponseMetadata>
                {
                    new HttpResponseMetadata<GroupRoleApiModel>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "OK"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
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
                }).SecurityRequirement(OAuth2ReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "DeleteGroup",
                "",
                "Deletes a group",
                new List<HttpResponseMetadata>
                {
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.NoContent,
                        Message = "Group deleted"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
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
                }).SecurityRequirement(OAuth2WriteScopeBuilder);

            #region Group -> Role Mapping Docs

            RouteDescriber.DescribeRouteWithParams(
                "GetRolesFromGroup",
                "",
                "Gets roles for a group by group name",
                new List<HttpResponseMetadata>
                {
                    new HttpResponseMetadata<GroupRoleApiModel>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "OK"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "Group with specified name was not found"
                    }
                },
                new[]
                {
                    _groupNameParameter,
                    _securableItemParameter,
                    _grainParameter
                },
                new[]
                {
                    _groupsTag
                }).SecurityRequirement(OAuth2ReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "AddRoleToGroup",
                "",
                "Adds a role to a group",
                new List<HttpResponseMetadata>
                {
                    new HttpResponseMetadata<GroupRoleApiModel>
                    {
                        Code = (int) HttpStatusCode.Created,
                        Message = "Created"
                    },
                    new HttpResponseMetadata
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "Group with specified name was not found or the role was not found"
                    }
                },
                new[]
                {
                    _groupNameParameter,
                    _roleIdParameter
                },
                new[]
                {
                    _groupsTag
                }).SecurityRequirement(OAuth2WriteScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "DeleteRoleFromGroup",
                "",
                "Deletes a role from a group",
                new List<HttpResponseMetadata>
                {
                    new HttpResponseMetadata<GroupRoleApiModel>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "Updated group entity including any mapped roles"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "Group with specified name was not found or the role was not found"
                    }
                },
                new[]
                {
                    _groupNameParameter,
                    _roleIdParameter
                },
                new[]
                {
                    _groupsTag
                }).SecurityRequirement(OAuth2WriteScopeBuilder);

            #endregion

            #region Group -> User Mapping Docs

            RouteDescriber.DescribeRouteWithParams(
                "GetUsersFromGroup",
                "",
                "Gets users for a custom group by group name",
                new List<HttpResponseMetadata>
                {
                    new HttpResponseMetadata<GroupUserApiModel>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "OK"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
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
                }).SecurityRequirement(OAuth2ReadScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "AddUserToGroup",
                "1) This operation is only valid for custom groups. 2) The user specified by SubjectId parameter will be added silently if not found.",
                "Adds a user to a group.",
                new List<HttpResponseMetadata>
                {
                    new HttpResponseMetadata<GroupUserApiModel>
                    {
                        Code = (int) HttpStatusCode.Created,
                        Message = "Created"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "Group with specified name was not found"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.BadRequest,
                        Message = "Group is not a custom group"
                    }
                },
                new[]
                {
                    _groupNameParameter,
                    _subjectIdParameter
                },
                new[]
                {
                    _groupsTag
                }).SecurityRequirement(OAuth2WriteScopeBuilder);

            RouteDescriber.DescribeRouteWithParams(
                "DeleteUserFromGroup",
                "",
                "Deletes a user from a group",
                new List<HttpResponseMetadata>
                {
                    new HttpResponseMetadata<GroupUserApiModel>
                    {
                        Code = (int) HttpStatusCode.OK,
                        Message = "Updated group entity including any mapped users"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.Forbidden,
                        Message = "Client does not have access"
                    },
                    new HttpResponseMetadata<Error>
                    {
                        Code = (int) HttpStatusCode.NotFound,
                        Message = "Group with specified name was not found or the user was not found"
                    }
                },
                new[]
                {
                    _groupNameParameter,
                    _subjectIdParameter
                },
                new[]
                {
                    _groupsTag
                }).SecurityRequirement(OAuth2WriteScopeBuilder);

            #endregion
        }
    }
}
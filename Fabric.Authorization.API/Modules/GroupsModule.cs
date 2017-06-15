using System;
using System.Linq;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Services;
using Nancy;
using Nancy.ModelBinding;

namespace Fabric.Authorization.API.Modules
{
    public class GroupsModule : NancyModule
    {
        private IGroupService GroupService { get; }

        public GroupsModule(IGroupService groupService) : base("/groups")
        {
            this.GroupService = groupService;

            base.Post("/", _ => this.AddGroup());
            base.Get("/{groupName}", _ => this.GetGroup());
            base.Delete("/{groupName}", _ => this.DeleteGroup());

            base.Get("/{groupName}/roles", _ => this.GetRolesFromGroup());
            base.Post("/{groupName}/roles", p => this.AddRoleToGroup(p));
            base.Delete("/{groupName}/roles", p => this.DeleteRoleFromGroup(p));
        }

        private dynamic GetGroup()
        {
            try
            {
                var group = this.GroupService.GetGroup(this.Bind<GroupRoleApiModel>().GroupName);
                return group.ToGroupRoleApiModel();
            }
            catch (GroupNotFoundException)
            {
                return HttpStatusCode.NotFound;
            }
        }

        private dynamic DeleteGroup()
        {
            try
            {
                this.GroupService.DeleteGroup(this.Bind<GroupRoleApiModel>().GroupName);
                return HttpStatusCode.NoContent;
            }
            catch (GroupNotFoundException)
            {
                return HttpStatusCode.NotFound;
            }
        }

        private HttpStatusCode AddGroup()
        {
            try
            {
                var group = this.Bind<GroupRoleApiModel>();
                this.GroupService.AddGroup(group.ToGroupDomainModel());
                return HttpStatusCode.NoContent;
            }
            catch (GroupAlreadyExistsException)
            {
                return HttpStatusCode.BadRequest;
            }
        }


        private dynamic GetRolesFromGroup()
        {
            try
            {
                var groupInfoRequest = this.Bind<GroupInfoRequest>();
                var roles = this.GroupService.GetRolesForGroup(groupInfoRequest.GroupName, groupInfoRequest.Grain,
                    groupInfoRequest.SecurableItem);

                return new GroupRoleApiModel
                {
                    RequestedGrain = groupInfoRequest.Grain,
                    RequestedSecurableItem = groupInfoRequest.SecurableItem,
                    GroupName = groupInfoRequest.GroupName,
                    Roles = roles.Select(r => r.ToRoleApiModel())
                };
            }
            catch (GroupNotFoundException)
            {
                return HttpStatusCode.NotFound;
            }
        }

        private HttpStatusCode AddRoleToGroup(dynamic parameters)
        {
            try
            {
                var roleApiModel = this.Bind<RoleApiModel>();
                if (roleApiModel.Id == null)
                {
                    throw new RoleNotFoundException();
                }

                this.GroupService.AddRoleToGroup(parameters.groupName, roleApiModel.Id.Value);
                return HttpStatusCode.NoContent;
            }
            catch (GroupNotFoundException)
            {
                return HttpStatusCode.NotFound;
            }
            catch (RoleNotFoundException)
            {
                return HttpStatusCode.BadRequest;
            }
        }

        private HttpStatusCode DeleteRoleFromGroup(dynamic parameters)
        {
            try
            {
                var roleApiModel = this.Bind<RoleApiModel>();
                if (roleApiModel.Id == null)
                {
                    throw new RoleNotFoundException();
                }

                this.GroupService.DeleteRoleFromGroup(parameters.groupName, roleApiModel.Id.Value);
                return HttpStatusCode.NoContent;
            }
            catch (GroupNotFoundException)
            {
                return HttpStatusCode.NotFound;
            }
            catch (RoleNotFoundException)
            {
                return HttpStatusCode.BadRequest;
            }
        }
    }
}
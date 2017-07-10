using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class GroupsModule : FabricModule<Group>
    {
        private IGroupService GroupService { get; }

        public GroupsModule(IGroupService groupService, GroupValidator validator, ILogger logger) : base(
            "/Groups", logger, validator)
        {
            this.GroupService = groupService;

            base.Post("/", _ => this.AddGroup());
            base.Post("/UpdateGroups", _ => this.UpdateGroupList());
            base.Get("/{groupName}", p => this.GetGroup(p));
            base.Delete("/{groupName}", p => this.DeleteGroup(p));

            base.Get("/{groupName}/roles", _ => this.GetRolesFromGroup());
            base.Post("/{groupName}/roles", p => this.AddRoleToGroup(p));
            base.Delete("/{groupName}/roles", p => this.DeleteRoleFromGroup(p));
        }

        private dynamic GetGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(this.AuthorizationManageClientsClaim, this.AuthorizationReadClaim);
                Group group = this.GroupService.GetGroup(parameters.groupName);
                return group.ToGroupRoleApiModel();
            }
            catch (NotFoundException<Group>)
            {
                return HttpStatusCode.NotFound;
            }
        }

        private dynamic DeleteGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(this.AuthorizationManageClientsClaim, this.AuthorizationWriteClaim);
                Group group = GroupService.GetGroup(parameters.groupName);
                this.GroupService.DeleteGroup(group);
                return HttpStatusCode.NoContent;
            }
            catch (NotFoundException<Group>)
            {
                return HttpStatusCode.NotFound;
            }
        }

        private HttpStatusCode AddGroup()
        {
            try
            {
                this.RequiresClaims(this.AuthorizationManageClientsClaim, this.AuthorizationWriteClaim);
                var group = this.Bind<GroupRoleApiModel>();
                this.GroupService.AddGroup(group.ToGroupDomainModel());
                return HttpStatusCode.Created;
            }
            catch (AlreadyExistsException<Group>)
            {
                return HttpStatusCode.BadRequest;
            }
        }

        private HttpStatusCode UpdateGroupList()
        {
            try
            {
                this.RequiresClaims(this.AuthorizationManageClientsClaim, this.AuthorizationWriteClaim);
                var group = this.Bind<List<GroupRoleApiModel>>();
                this.GroupService.UpdateGroupList(group.Select(g => g.ToGroupDomainModel()).ToList());
                return HttpStatusCode.NoContent;
            }
            catch (AlreadyExistsException<Group>)
            {
                return HttpStatusCode.BadRequest;
            }
        }

        private dynamic GetRolesFromGroup()
        {
            try
            {
                this.RequiresClaims(this.AuthorizationReadClaim);
                var groupInfoRequest = this.Bind<GroupInfoRequest>();
                var roles = this.GroupService.GetRolesForGroup(groupInfoRequest.GroupName, groupInfoRequest.Grain,
                    groupInfoRequest.SecurableItem);

                return new GroupRoleApiModel
                {
                   // RequestedGrain = groupInfoRequest.Grain,
                   // RequestedSecurableItem = groupInfoRequest.SecurableItem,
                    GroupName = groupInfoRequest.GroupName,
                    Roles = roles.Select(r => r.ToRoleApiModel())
                };
            }
            catch (NotFoundException<Group>)
            {
                return HttpStatusCode.NotFound;
            }
        }

        private HttpStatusCode AddRoleToGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(this.AuthorizationWriteClaim);
                var roleApiModel = this.Bind<RoleApiModel>();
                if (roleApiModel.Id == null)
                {
                    throw new NotFoundException<Role>();
                }

                this.GroupService.AddRoleToGroup(parameters.groupName, roleApiModel.Id.Value);
                return HttpStatusCode.NoContent;
            }
            catch (NotFoundException<Group>)
            {
                return HttpStatusCode.NotFound;
            }
            catch (NotFoundException<Role>)
            {
                return HttpStatusCode.BadRequest;
            }
        }

        private HttpStatusCode DeleteRoleFromGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(this.AuthorizationWriteClaim);
                var roleApiModel = this.Bind<RoleApiModel>();
                if (roleApiModel.Id == null)
                {
                    throw new NotFoundException<Role>();
                }

                this.GroupService.DeleteRoleFromGroup(parameters.groupName, roleApiModel.Id.Value);
                return HttpStatusCode.NoContent;
            }
            catch (NotFoundException<Group>)
            {
                return HttpStatusCode.NotFound;
            }
            catch (NotFoundException<Role>)
            {
                return HttpStatusCode.BadRequest;
            }
        }
    }
}
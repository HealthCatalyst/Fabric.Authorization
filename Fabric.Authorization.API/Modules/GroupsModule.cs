using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private GroupService _groupService { get; }

        public GroupsModule(GroupService groupService, GroupValidator validator, ILogger logger) : base(
            "/Groups", logger, validator)
        {
            _groupService = groupService;

            base.Post("/", async _ => await this.AddGroup());
            base.Post("/UpdateGroups", async _ => await this.UpdateGroupList().ConfigureAwait(false));
            base.Get("/{groupName}", async p => await this.GetGroup(p).ConfigureAwait(false));
            base.Delete("/{groupName}", async p => await this.DeleteGroup(p).ConfigureAwait(false));

            base.Get("/{groupName}/roles", async _ => await this.GetRolesFromGroup().ConfigureAwait(false));
            base.Post("/{groupName}/roles", async p => await this.AddRoleToGroup(p).ConfigureAwait(false));
            base.Delete("/{groupName}/roles", async p => await this.DeleteRoleFromGroup(p).ConfigureAwait(false));
        }

        private async Task<dynamic> GetGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(this.AuthorizationManageClientsClaim, this.AuthorizationReadClaim);
                Group group = await _groupService.GetGroup(parameters.groupName);
                return group.ToGroupRoleApiModel();
            }
            catch (NotFoundException<Group>)
            {
                return HttpStatusCode.NotFound;
            }
        }

        private async Task<dynamic> DeleteGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(this.AuthorizationManageClientsClaim, this.AuthorizationWriteClaim);
                Group group = await _groupService.GetGroup(parameters.groupName);
                await _groupService.DeleteGroup(group);
                return HttpStatusCode.NoContent;
            }
            catch (NotFoundException<Group>)
            {
                return HttpStatusCode.NotFound;
            }
        }

        private async Task<HttpStatusCode> AddGroup()
        {
            try
            {
                this.RequiresClaims(this.AuthorizationManageClientsClaim, this.AuthorizationWriteClaim);
                var group = this.Bind<GroupRoleApiModel>();
                await _groupService.AddGroup(group.ToGroupDomainModel());
                return HttpStatusCode.Created;
            }
            catch (AlreadyExistsException<Group>)
            {
                return HttpStatusCode.BadRequest;
            }
        }

        private async Task<HttpStatusCode> UpdateGroupList()
        {
            try
            {
                this.RequiresClaims(this.AuthorizationManageClientsClaim, this.AuthorizationWriteClaim);
                var group = this.Bind<List<GroupRoleApiModel>>();
                await _groupService.UpdateGroupList(group.Select(g => g.ToGroupDomainModel()));
                return HttpStatusCode.NoContent;
            }
            catch (AlreadyExistsException<Group>)
            {
                return HttpStatusCode.BadRequest;
            }
        }

        private async Task<dynamic> GetRolesFromGroup()
        {
            try
            {
                this.RequiresClaims(this.AuthorizationReadClaim);
                var groupInfoRequest = this.Bind<GroupInfoRequest>();
                var roles = await _groupService.GetRolesForGroup(groupInfoRequest.GroupName, groupInfoRequest.Grain, groupInfoRequest.SecurableItem);

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

        private async Task<HttpStatusCode> AddRoleToGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(this.AuthorizationWriteClaim);
                var roleApiModel = this.Bind<RoleApiModel>();
                if (roleApiModel.Id == null)
                {
                    throw new NotFoundException<Role>();
                }

                await _groupService.AddRoleToGroup(parameters.groupName, roleApiModel.Id.Value);
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

        private async Task<HttpStatusCode> DeleteRoleFromGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(this.AuthorizationWriteClaim);
                var roleApiModel = this.Bind<RoleApiModel>();
                if (roleApiModel.Id == null)
                {
                    throw new NotFoundException<Role>();
                }

                await _groupService.DeleteRoleFromGroup(parameters.groupName, roleApiModel.Id.Value);
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
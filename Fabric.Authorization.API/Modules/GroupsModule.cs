using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class GroupsModule : FabricModule<Group>
    {
        private readonly GroupService _groupService;

        public GroupsModule(GroupService groupService, GroupValidator validator, ILogger logger) : base(
            "/v1/Groups", logger, validator)
        {
            _groupService = groupService;

            Post("/", async _ => await AddGroup(), null, "AddGroup");

            Post("/UpdateGroups", async _ => await UpdateGroupList().ConfigureAwait(false), null, "UpdateGroups");

            base.Get("/{groupName}", async p => await this.GetGroup(p).ConfigureAwait(false), null, "GetGroup");

            base.Delete("/{groupName}", async p => await this.DeleteGroup(p).ConfigureAwait(false), null,
                "DeleteGroup");

            Get("/{groupName}/roles", async _ => await GetRolesFromGroup().ConfigureAwait(false), null,
                "GetRolesFromGroup");

            base.Post("/{groupName}/roles", async p => await this.AddRoleToGroup(p).ConfigureAwait(false), null,
                "AddRoleToGroup");

            base.Delete("/{groupName}/roles", async p => await this.DeleteRoleFromGroup(p).ConfigureAwait(false), null,
                "DeleteRoleFromGroup");

            base.Post("/{groupName}/roles", async p => await this.AddRoleToGroup(p).ConfigureAwait(false), null,
                "AddRoleToGroup");
        }

        private async Task<dynamic> GetGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationReadClaim);
                Group group = await _groupService.GetGroup(parameters.groupName);
                return group.ToGroupRoleApiModel();
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> DeleteGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationWriteClaim);
                Group group = await _groupService.GetGroup(parameters.groupName);
                await _groupService.DeleteGroup(group);
                return HttpStatusCode.NoContent;
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> AddGroup()
        {
            this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationWriteClaim);
            var group = this.Bind<GroupRoleApiModel>();
            var incomingGroup = group.ToGroupDomainModel();
            Validate(incomingGroup);

            try
            {
                var createdGroup = await _groupService.AddGroup(incomingGroup);
                return CreateSuccessfulPostResponse(createdGroup.ToGroupRoleApiModel());
            }
            catch (AlreadyExistsException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
        }

        private async Task<dynamic> UpdateGroupList()
        {
            try
            {
                this.RequiresClaims(AuthorizationManageClientsClaim, AuthorizationWriteClaim);
                var group = this.Bind<List<GroupRoleApiModel>>();
                await _groupService.UpdateGroupList(group.Select(g => g.ToGroupDomainModel()));
                return HttpStatusCode.NoContent;
            }
            catch (AlreadyExistsException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
        }

        private async Task<dynamic> GetRolesFromGroup()
        {
            try
            {
                this.RequiresClaims(AuthorizationReadClaim);
                var groupInfoRequest = this.Bind<GroupInfoRequest>();
                var roles = await _groupService.GetRolesForGroup(groupInfoRequest.GroupName, groupInfoRequest.Grain,
                    groupInfoRequest.SecurableItem);

                return new GroupRoleApiModel
                {
                    // RequestedGrain = groupInfoRequest.Grain,
                    // RequestedSecurableItem = groupInfoRequest.SecurableItem,
                    GroupName = groupInfoRequest.GroupName,
                    Roles = roles.Select(r => r.ToRoleApiModel())
                };
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> AddRoleToGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(AuthorizationWriteClaim);
                var roleApiModel = this.Bind<RoleApiModel>();
                if (roleApiModel.Id == null)
                    throw new NotFoundException<Role>();

                Group group = await _groupService.AddRoleToGroup(parameters.groupName, roleApiModel.Id.Value);
                return CreateSuccessfulPostResponse(group.ToGroupRoleApiModel());
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (NotFoundException<Role> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> DeleteRoleFromGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(AuthorizationWriteClaim);
                var roleApiModel = this.Bind<RoleApiModel>();
                if (roleApiModel.Id == null)
                    throw new NotFoundException<Role>();

                Group group = await _groupService.DeleteRoleFromGroup(parameters.groupName, roleApiModel.Id.Value);
                return CreateSuccessfulPostResponse(group.ToGroupRoleApiModel(), HttpStatusCode.OK);
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (NotFoundException<Role> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }
    }
}
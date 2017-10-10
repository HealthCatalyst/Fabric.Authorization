using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;
using Nancy.Security;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class GroupsModule : FabricModule<Group>
    {
        private readonly GroupService _groupService;

        public GroupsModule(
            GroupService groupService,
            GroupValidator validator,
            ILogger logger,
            IPropertySettings propertySettings = null) : base("/v1/groups", logger, validator, propertySettings)
        {
            _groupService = groupService;

            Post("/", async _ => await AddGroup(), null, "AddGroup");

            Post("/UpdateGroups", async _ => await UpdateGroupList().ConfigureAwait(false), null, "UpdateGroups");

            base.Get("/{groupName}", async p => await this.GetGroup(p).ConfigureAwait(false), null, "GetGroup");

            base.Delete("/{groupName}", async p => await this.DeleteGroup(p).ConfigureAwait(false), null,
                "DeleteGroup");

            // group->role mappings
            Get("/{groupName}/roles", async _ => await GetRolesFromGroup().ConfigureAwait(false), null,
                "GetRolesFromGroup");

            Post("/{groupName}/roles", async _ => await AddRoleToGroup().ConfigureAwait(false), null,
                "AddRoleToGroup");

            Delete("/{groupName}/roles", async _ => await DeleteRoleFromGroup().ConfigureAwait(false), null,
                "DeleteRoleFromGroup");

            // (custom) group->user mappings
            Get("/{groupName}/users", async _ => await GetUsersFromGroup().ConfigureAwait(false), null,
                "GetUsersFromGroup");

            Post("/{groupName}/users", async _ => await AddUserToGroup().ConfigureAwait(false), null, "AddUserToGroup");

            Delete("/{groupName}/users", async _ => await DeleteUserFromGroup().ConfigureAwait(false), null,
                "DeleteUserFromGroup");
        }

        private async Task<dynamic> GetGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(AuthorizationReadClaim);
                Group group = await _groupService.GetGroup(parameters.groupName, ClientId);
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
                this.RequiresClaims(AuthorizationWriteClaim);
                Group group = await _groupService.GetGroup(parameters.groupName, ClientId);
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
            this.RequiresClaims(AuthorizationWriteClaim);
            var group = this.Bind<GroupRoleApiModel>();
            var incomingGroup = group.ToGroupDomainModel();

            if (string.IsNullOrWhiteSpace(incomingGroup.Source))
            {
                incomingGroup.Source = PropertySettings?.GroupSource;
            }

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
                this.RequiresClaims(AuthorizationWriteClaim);
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
                var groupRoleRequest = this.Bind<GroupRoleRequest>();
                var group = await _groupService.GetGroup(groupRoleRequest.GroupName, ClientId);
                return group.ToGroupRoleApiModel(groupRoleRequest, GroupService.GroupRoleFilter);
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> AddRoleToGroup()
        {
            try
            {
                this.RequiresClaims(AuthorizationWriteClaim);
                var groupRoleRequest = this.Bind<GroupRoleRequest>();
                if (groupRoleRequest.Id == null && groupRoleRequest.RoleId == null)
                {
                    return CreateFailureResponse("Role ID is required.", HttpStatusCode.BadRequest);
                }

                var roleId = groupRoleRequest.Id ?? groupRoleRequest.RoleId;
                var group = await _groupService.AddRoleToGroup(groupRoleRequest.GroupName, roleId.Value);
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

        private async Task<dynamic> DeleteRoleFromGroup()
        {
            try
            {
                this.RequiresClaims(AuthorizationWriteClaim);
                var groupRoleRequest = this.Bind<GroupRoleRequest>();
                if (groupRoleRequest.Id == null && groupRoleRequest.RoleId == null)
                {
                    return CreateFailureResponse("Role ID is required.", HttpStatusCode.BadRequest);
                }

                var roleId = groupRoleRequest.Id ?? groupRoleRequest.RoleId;
                var group = await _groupService.DeleteRoleFromGroup(groupRoleRequest.GroupName, roleId.Value);
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

        private async Task<dynamic> GetUsersFromGroup()
        {
            try
            {
                this.RequiresClaims(AuthorizationReadClaim);
                var groupUserRequest = this.Bind<GroupUserRequest>();
                var group = await _groupService.GetGroup(groupUserRequest.GroupName, ClientId);
                return group.ToGroupUserApiModel();
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> AddUserToGroup()
        {
            try
            {
                this.RequiresClaims(AuthorizationWriteClaim);
                var groupUserRequest = this.Bind<GroupUserRequest>();
                var validationResult = ValidateGroupUserRequest(groupUserRequest);
                if (validationResult != null)
                {
                    return validationResult;
                }

                if (string.IsNullOrWhiteSpace(groupUserRequest.SubjectId))
                {
                    return CreateFailureResponse("subjectId is required", HttpStatusCode.BadRequest);
                }
            
                if (string.IsNullOrWhiteSpace(groupUserRequest.IdentityProvider))
                {
                    return CreateFailureResponse("identityProvider is required", HttpStatusCode.BadRequest);
                }

                var group = await _groupService.AddUserToGroup(groupUserRequest.GroupName, groupUserRequest.SubjectId, groupUserRequest.IdentityProvider);
                return CreateSuccessfulPostResponse(group.ToGroupUserApiModel());
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (NotFoundException<User> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (BadRequestException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
        }

        private async Task<dynamic> DeleteUserFromGroup()
        {
            try
            {
                this.RequiresClaims(AuthorizationWriteClaim);
                var groupUserRequest = this.Bind<GroupUserRequest>();
                var validationResult = ValidateGroupUserRequest(groupUserRequest);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var group = await _groupService.DeleteUserFromGroup(groupUserRequest.GroupName, groupUserRequest.SubjectId, groupUserRequest.IdentityProvider);
                return CreateSuccessfulPostResponse(group.ToGroupUserApiModel(), HttpStatusCode.OK);
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (NotFoundException<User> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }

        private Negotiator ValidateGroupUserRequest(GroupUserRequest groupUserRequest)
        {
            if (string.IsNullOrWhiteSpace(groupUserRequest.SubjectId))
            {
                return CreateFailureResponse("subjectId is required", HttpStatusCode.BadRequest);
            }
            if (string.IsNullOrWhiteSpace(groupUserRequest.IdentityProvider))
            {
                return CreateFailureResponse("identityProvider is required", HttpStatusCode.BadRequest);
            }

            return null;
        }
    }
}
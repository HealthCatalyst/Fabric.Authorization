using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Catalyst.Fabric.Authorization.Models;
using Catalyst.Fabric.Authorization.Models.Requests;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
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
        private readonly ClientService _clientService;
        private readonly GrainService _grainService;
        private readonly IdPSearchService _idPSearchService;

        public static string InvalidRoleArrayMessage =
            "No roles present in payload; please ensure you are posting an array of RoleApiModels.";

        public static string InvalidRoleApiModelMessage =
            "The role: {0} is missing an Id, Grain or SecurableItem and cannot be added";

        public static string InvalidGroupUserRequestArrayMessage =
            "No users present in payload; please ensure you are posting an array of GroupUserRequest entities.";

        public static string InvalidGroupUserRequestMessage =
            "One or more entities in the GroupUserRequest array is missing a value for identityProvider and/or subjectId.";

        public GroupsModule(
            GroupService groupService,
            GroupValidator validator,
            ILogger logger,
            AccessService accessService,
            ClientService clientService,
            GrainService grainService,
            IdPSearchService idPSearchService,
            IAppConfiguration appConfiguration = null) : base("/v1/groups", logger, validator, accessService, appConfiguration)
        {
            _groupService = groupService;
            _clientService = clientService;
            _grainService = grainService;
            _idPSearchService = idPSearchService;

            Get("/",
            async _ => await GetGroups().ConfigureAwait(false),
            null,
            "GetGroups");

            base.Get("/{groupName}",
                async p => await this.GetGroup(p).ConfigureAwait(false),
                null,
                "GetGroup");

            Post("/", 
                async _ => await AddGroup().ConfigureAwait(false),
                null,
                "AddGroup");

            Post("/UpdateGroups", 
                async _ => await UpdateGroupList().ConfigureAwait(false),
                null,
                "UpdateGroups");

            Patch("/{groupName}",
                async parameters => await this.UpdateGroup(parameters).ConfigureAwait(false),
                null,
                "UpdateGroup");

            base.Delete("/{groupName}",
                async p => await this.DeleteGroup(p).ConfigureAwait(false),
                null,
                "DeleteGroup");

            // group->role mappings
            Get("/{groupName}/roles",
                async _ => await GetRolesFromGroup().ConfigureAwait(false),
                null,
                "GetRolesFromGroup");

            Get("/{groupName}/{grain}/{securableItem}/roles",
                async p => await GetRolesForGroup(p).ConfigureAwait(false),
                null,
                "GetRolesForGroup");

            Post("/{groupName}/roles",
                async p => await AddRolesToGroup(p).ConfigureAwait(false),
                null,
                "AddRolesToGroup");

            Delete("/{groupName}/roles",
                async p => await DeleteRolesFromGroup(p).ConfigureAwait(false),
                null,
                "DeleteRolesFromGroup");

            // (custom) group->user mappings
            Get("/{groupName}/users",
                async _ => await GetUsersFromGroup().ConfigureAwait(false),
                null,
                "GetUsersFromGroup");

            Post("/{groupName}/users",
                async p => await AddUsersToGroup(p).ConfigureAwait(false),
                null,
                "AddUserToGroup");

            Delete("/{groupName}/users",
                async _ => await DeleteUserFromGroup().ConfigureAwait(false),
                null,
                "DeleteUserFromGroup");

            // child groups
            Get("/{groupName}/groups",
                async p => await GetChildGroups(p).ConfigureAwait(false),
                null,
                "GetChildGroups");

            Post("/{groupName}/groups",
                async p => await AddChildGroups(p).ConfigureAwait(false),
                null,
                "AddChildGroups");

            Delete("/{groupName}/groups",
                async p => await RemoveChildGroups(p).ConfigureAwait(false),
                null,
                "RemoveChildGroups");
        }

        private async Task<dynamic> GetGroups()
        {
            try
            {
                this.RequiresClaims(AuthorizationReadClaim);
                var requestParams = this.Bind<GroupSearchApiRequest>();
                if (string.IsNullOrEmpty(requestParams.Name))
                {
                   return CreateFailureResponse("Name is required", HttpStatusCode.BadRequest);
                }
                IEnumerable<Group> groups = await _groupService.GetGroups(requestParams.Name, requestParams.Type);
                return groups.OrderBy(g => g.Name).Select(g => g.ToGroupRoleApiModel());
            }
            catch (InvalidOperationException ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
        }

        private async Task<dynamic> GetRolesForGroup(dynamic p)
        {
            try
            {
                this.RequiresClaims(AuthorizationReadClaim);
                string groupName = p.groupName;
                string grain = p.grain;
                string securableItem = p.securableItem;
                var group = await _groupService.GetGroup(groupName);
                return group.Roles.ToRoleApiModels(grain, securableItem, GroupService.RoleFilter);
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
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

        private async Task<dynamic> AddGroup()
        {
            this.RequiresClaims(AuthorizationWriteClaim);
            var group = this.Bind<GroupRoleApiModel>(binderIgnore => binderIgnore.Id);
            var incomingGroup = group.ToGroupDomainModel();

            if (string.IsNullOrWhiteSpace(incomingGroup.Source))
            {
                incomingGroup.Source = AppConfiguration.DefaultPropertySettings?.GroupSource;
            }

            if (string.IsNullOrWhiteSpace(incomingGroup.IdentityProvider))
            {
                incomingGroup.IdentityProvider = AppConfiguration.DefaultPropertySettings?.IdentityProvider;
            }

            Validate(incomingGroup);

            try
            {
                if (string.Equals(incomingGroup.IdentityProvider, IdentityConstants.AzureActiveDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    var idPSearchResponse = await _idPSearchService.GetGroup(incomingGroup.Name, incomingGroup.Tenant);
                    if (!idPSearchResponse.Result.Principals.Any())
                    {
                        return CreateFailureResponse(
                            $"Group name {incomingGroup.Name} from {incomingGroup.IdentityProvider} tenant {incomingGroup.Tenant} was not found in the external identity provider directory.",
                            HttpStatusCode.BadRequest);
                    }

                    incomingGroup.ExternalIdentifier = idPSearchResponse.Result.Principals.First().ExternalIdentifier;
                }

                var createdGroup = await _groupService.AddGroup(incomingGroup);
                return CreateSuccessfulPostResponse(createdGroup.ToGroupRoleApiModel());
            }
            catch (AlreadyExistsException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.Conflict);
            }
        }

        private async Task<dynamic> UpdateGroup(dynamic parameters)
        {
            this.RequiresClaims(AuthorizationWriteClaim);

            try
            {
                var groupPatchApiRequest = this.Bind<GroupPatchApiRequest>();

                var existingGroup = (Group) await _groupService.GetGroup(parameters.groupName);
                existingGroup.DisplayName = groupPatchApiRequest.DisplayName;
                existingGroup.Description = groupPatchApiRequest.Description;

                var group = await _groupService.UpdateGroup(existingGroup);
                return CreateSuccessfulPatchResponse(group.ToGroupRoleApiModel());
            }
            catch (NotFoundException<Group> ex)
            {
                Logger.Error(ex, ex.Message, parameters.groupName);
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

        private async Task<dynamic> UpdateGroupList()
        {
            try
            {
                this.RequiresClaims(AuthorizationWriteClaim);
                var groups = this.Bind<List<GroupRoleApiModel>>();
                await _groupService.UpdateGroupList(groups.Select(g => g.ToGroupDomainModel()));
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
                return group.Roles.ToRoleApiModels(groupRoleRequest.Grain, groupRoleRequest.SecurableItem, GroupService.RoleFilter);
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> AddRolesToGroup(dynamic parameters)
        {
            var apiRoles = this.Bind<List<RoleApiModel>>();
            var errorResponse = await ValidateRoles(apiRoles);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            var domainRoles = apiRoles.Select(r => r.ToRoleDomainModel()).ToList();
            await CheckWriteAccess(domainRoles);

            try
            {
                Group group = await _groupService.AddRolesToGroup(domainRoles, parameters.groupName);
                return CreateSuccessfulPostResponse(group.Name, group.ToGroupRoleApiModel(),
                    HttpStatusCode.OK);
            }
            catch (NotFoundException<Group>)
            {
                return CreateFailureResponse(
                    $"Group with name {parameters.groupName} was not found",
                    HttpStatusCode.NotFound);
            }
            catch (AggregateException ex)
            {
                return CreateFailureResponse(ex, HttpStatusCode.BadRequest);
            }
        }

        private async Task<dynamic> DeleteRolesFromGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(AuthorizationWriteClaim);
                var roleIds = this.Bind<List<RoleIdentifierApiRequest>>();
                if (roleIds == null || roleIds.Count == 0)
                {
                    return CreateFailureResponse("At least 1 role ID is required.", HttpStatusCode.BadRequest);
                }

                Group group = await _groupService.DeleteRolesFromGroup(parameters.GroupName, roleIds.Select(r => r.RoleId));
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
                return group.Users.Where(u => !u.IsDeleted).Select(u => u.ToUserApiModel());
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> AddUsersToGroup(dynamic parameters)
        {
            try
            {
                Group group = await _groupService.GetGroup(parameters.GroupName);
                await CheckWriteAccess(group);
                var userApiRequests = this.Bind<List<UserIdentifierApiRequest>>();
                var validationResult = await ValidateGroupUserRequests(userApiRequests);
                if (validationResult != null)
                {
                    return validationResult;
                }
                
                group = await _groupService.AddUsersToGroup(parameters.GroupName, userApiRequests.Select(u => new User(u.SubjectId, u.IdentityProvider)).ToList());
                return CreateSuccessfulPostResponse(group.Name, group.ToGroupUserApiModel(), HttpStatusCode.OK);
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
            catch (AggregateException ex)
            {
                return CreateFailureResponse(ex, HttpStatusCode.BadRequest);
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

                var group = await _groupService.DeleteUserFromGroup(groupUserRequest.GroupName,
                    groupUserRequest.SubjectId, groupUserRequest.IdentityProvider);
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

        private async Task<dynamic> GetChildGroups(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(AuthorizationReadClaim);
                Group group = await _groupService.GetGroup(parameters.GroupName, ClientId);
                return group.Children.Select(g => g.ToGroupRoleApiModel());
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> AddChildGroups(dynamic parameters)
        {
            try
            {
                Group group = await _groupService.GetGroup(parameters.GroupName);
                await CheckWriteAccess(group);
                var groupPostApiRequests = this.Bind<List<GroupPostApiRequest>>();

                group = await _groupService.AddChildGroups(group, groupPostApiRequests.Select(g => g.ToGroupDomainModel()));
                return CreateSuccessfulPostResponse(group.Name, group.ToGroupRoleApiModel(), HttpStatusCode.OK);
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (BadRequestException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
            catch (AlreadyExistsException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.Conflict);
            }
        }

        private async Task<dynamic> RemoveChildGroups(dynamic parameters)
        {
            try
            {
                Group group = await _groupService.GetGroup(parameters.GroupName);
                await CheckWriteAccess(group);
                var groupIdentifiers = this.Bind<List<GroupIdentifierApiRequest>>();

                group = await _groupService.RemoveChildGroups(group, groupIdentifiers.Select(g => g.GroupName).ToList());
                return CreateSuccessfulPostResponse(group.Name, group.ToGroupRoleApiModel(), HttpStatusCode.OK);
            }
            catch (NotFoundException<Group> ex)
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

            return string.IsNullOrWhiteSpace(groupUserRequest.IdentityProvider)
                ? CreateFailureResponse("identityProvider is required", HttpStatusCode.BadRequest)
                : null;
        }

        private async Task<Negotiator> ValidateGroupUserRequests(IReadOnlyCollection<UserIdentifierApiRequest> userApiRequests)
        {
            if (userApiRequests.Count == 0)
            {
                return await CreateFailureResponse(InvalidGroupUserRequestArrayMessage, HttpStatusCode.BadRequest);
            }

            var invalidEntities = userApiRequests.Where(r =>
                    string.IsNullOrEmpty(r.IdentityProvider)
                    || string.IsNullOrEmpty(r.SubjectId));

            if (invalidEntities.Any())
            {
                return await CreateFailureResponse(InvalidGroupUserRequestMessage, HttpStatusCode.BadRequest);
            }
            return null;
        }

        private async Task<Negotiator> ValidateRoles(IReadOnlyCollection<RoleApiModel> apiRoles)
        {
            if (apiRoles.Count == 0)
            {
                return await CreateFailureResponse(InvalidRoleArrayMessage, HttpStatusCode.BadRequest);
            }

            var messages = apiRoles.Where(r =>
                    !r.Id.HasValue
                    || r.Id.Value == Guid.Empty
                    || string.IsNullOrEmpty(r.Grain)
                    || string.IsNullOrEmpty(r.SecurableItem))
                .Select(r => string.Format(InvalidRoleApiModelMessage, r.Name)).ToList();

            if (messages.Any())
            {
                return await CreateFailureResponse(messages, HttpStatusCode.BadRequest);
            }
            return null;
        }

        private async Task CheckWriteAccess(Group group)
        {
            if (group.Roles != null && group.Roles.Any())
            {
                await CheckWriteAccess(group.Roles);
            }
            else
            {
                this.RequiresClaims(AuthorizationWriteClaim);
            }
        }

        private async Task CheckWriteAccess(IEnumerable<Role> roles)
        {
            foreach (var role in roles)
            {
                await CheckWriteAccess(_clientService, _grainService, role.Grain, role.SecurableItem);
            }
        }
    }
}
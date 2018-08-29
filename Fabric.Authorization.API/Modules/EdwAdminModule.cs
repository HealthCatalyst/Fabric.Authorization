using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.Modules
{
    public class EdwAdminModule : FabricModule<User>
    {
        private readonly UserService _userService;
        private readonly GroupService _groupService;
        private readonly IEDWAdminRoleSyncService _syncService;

        public EdwAdminModule(
            UserValidator validator,
            AccessService accessService,
            UserService userService,
            GroupService groupService,
            IEDWAdminRoleSyncService syncService,
            ILogger logger) : base("/v1/edw/", logger, validator, accessService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));
            _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));

            Post("/{identityProvider}/{subjectId}/roles",
                async param => await SyncUserIdentityRoles(param).ConfigureAwait(false), null,
                "SyncUserIdentityRoles");

            Post("/{groupName}/roles",
                async param => await SyncGroupIdentityRoles(param).ConfigureAwait(false), null,
                "SyncGroupIdentityRoles");
        }

        private async Task<dynamic> SyncUserIdentityRoles(dynamic param)
        {
            CheckInternalAccess();

            try
            {
                User user = await _userService.GetUser(param.subjectId, param.identityProvider);
                await _syncService.RefreshDosAdminRolesAsync(user);
                return HttpStatusCode.NoContent;
            }
            catch (NotFoundException<User>)
            {
                return CreateFailureResponse($"The user: {param.subjectId} for identity provider: {param.identityProvider} was not found.", HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> SyncGroupIdentityRoles(dynamic param)
        {
            CheckInternalAccess();

            try
            {
                Group group = await _groupService.GetGroup(param.groupName);
                foreach(var groupUser in group.Users)
                {
                    var user = await _userService.GetUser(groupUser.SubjectId, groupUser.IdentityProvider);
                    await _syncService.RefreshDosAdminRolesAsync(user);
                }
                return HttpStatusCode.NoContent;
            }
            catch (NotFoundException<Group>)
            {
                return CreateFailureResponse($"TEMP Group not found", HttpStatusCode.NotFound);
            }
            catch (NotFoundException<User>)
            {
                return CreateFailureResponse($"The user: {param.subjectId} for identity provider: {param.identityProvider} was not found.", HttpStatusCode.NotFound);
            }
        }
    }
}

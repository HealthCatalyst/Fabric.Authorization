using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Nancy.Extensions;
using Serilog;

namespace Fabric.Authorization.API.Services
{
    public class MemberSearchService
    {
        private readonly ClientService _clientService;
        private readonly GroupService _groupService;
        private readonly IIdentityServiceProvider _identityServiceProvider;
        private readonly RoleService _roleService;
        private readonly ILogger _logger;

        public MemberSearchService(
            ClientService clientService,
            RoleService roleService,
            GroupService groupService,
            IIdentityServiceProvider identityServiceProvider,
            ILogger logger)
        {
            _clientService = clientService;
            _roleService = roleService;
            _groupService = groupService;
            _identityServiceProvider = identityServiceProvider;
            _logger = logger;
        }

        public async Task<FabricAuthUserSearchResponse> Search(MemberSearchRequest request)
        {
            var searchResults = new List<MemberSearchResponse>();

            if (string.IsNullOrWhiteSpace(request.ClientId))
            {
                throw new BadRequestException<MemberSearchRequest>("Client ID is required.");
            }

            var client = await _clientService.GetClient(request.ClientId);
            var clientRoles = await _roleService.GetRoles(client);

            var clientRoleEntities = clientRoles.ToList();
            _logger.Debug($"clientRoles = {clientRoleEntities.ToString(Environment.NewLine)}");
            if (clientRoleEntities.Count == 0)
            {
                return new FabricAuthUserSearchResponse
                {
                    HttpStatusCode = Nancy.HttpStatusCode.OK,
                    Results = new List<MemberSearchResponse>()
                };
            }

            // get all groups tied to clientRoles
            var groupIds = clientRoleEntities.SelectMany(r => r.Groups).Distinct().ToList();
            _logger.Debug($"groupIds = {groupIds.ToString(Environment.NewLine)}");

            if (groupIds.Count == 0)
            {
                return new FabricAuthUserSearchResponse
                {
                    HttpStatusCode = Nancy.HttpStatusCode.OK,
                    Results = new List<MemberSearchResponse>()
                };
            }

            var groupEntities = new List<Group>();
            foreach (var groupId in groupIds)
            {
                try
                {
                    var group = await _groupService.GetGroup(groupId, request.ClientId);
                    groupEntities.Add(group);
                }
                catch (NotFoundException<Group> ex)
                {
                    _logger.Error($"{ex.Message} (Group is mapped to at least 1 valid role)");
                }
            }

            _logger.Debug($"groupEntities = {groupEntities.ToString(Environment.NewLine)}");

            var groupsMappedToClientRoles = groupEntities.Where(g => g.Roles.Any(r => clientRoleEntities.Contains(r))).ToList();
            _logger.Debug($"groupsMappedToClientRoles = {groupsMappedToClientRoles.ToString(Environment.NewLine)}");

            // add all non-custom groups to the response
            searchResults.AddRange(groupsMappedToClientRoles.Select(g => new MemberSearchResponse
            {
                GroupName = g.Name,
                Roles = g.Roles.Select(r => r.ToRoleApiModel()),
                EntityType = MemberSearchResponseEntityType.Group.ToString()
            }));

            // get all users mapped to groups in client roles
            var users = groupsMappedToClientRoles
                .Where(g => g.Users != null && g.Users.Count > 0)
                .SelectMany(g => g.Users)
                .DistinctBy(u => u.SubjectId)
                .ToList();

           users.AddRange(clientRoleEntities
               .SelectMany(r => r.Users).Distinct());

            var userList = new List<MemberSearchResponse>();

            foreach (var user in users)
            {
                // get groups for user
                var userGroupEntities = groupEntities.Where(g => user.Groups.Contains(g));

                // get roles for user
                var userRoles = userGroupEntities.SelectMany(g => g.Roles)
                    //.Select(r => r.Name)
                    .ToList();
                userRoles.AddRange(clientRoleEntities
                    .Where(r => r.Users.Any(u => u.IdentityProvider.Equals(user.IdentityProvider, StringComparison.OrdinalIgnoreCase ) &&
                                                 u.SubjectId.Equals(user.SubjectId, StringComparison.OrdinalIgnoreCase)))
                    //.Select(r => r.Name)
                    .ToList());

                // add user to response
                userList.Add(new MemberSearchResponse
                {
                    SubjectId = user.SubjectId,
                    IdentityProvider = user.IdentityProvider,
                    Roles = userRoles.Select(r => r.ToRoleApiModel()),
                    EntityType = MemberSearchResponseEntityType.User.ToString()
                });
            }

            var fabricIdentityUserResponse =
                await _identityServiceProvider.Search(request.ClientId, userList.Select(u => $"{u.SubjectId}:{u.IdentityProvider}"));

            if (fabricIdentityUserResponse != null && fabricIdentityUserResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                // update user details with Fabric.Identity response
                foreach (var user in fabricIdentityUserResponse.Results)
                {
                    var userSearchResponse = userList.FirstOrDefault(u => string.Equals(u.SubjectId, user.SubjectId, StringComparison.OrdinalIgnoreCase));
                    if (userSearchResponse == null)
                    {
                        continue;
                    }

                    userSearchResponse.FirstName = user.FirstName;
                    userSearchResponse.MiddleName = user.MiddleName;
                    userSearchResponse.LastName = user.LastName;
                    userSearchResponse.LastLoginDateTimeUtc = user.LastLoginDate;
                }
            }

            searchResults.AddRange(userList);

            _logger.Debug($"searchResults = {searchResults.ToString(Environment.NewLine)}");

            var pageSize = request.PageSize ?? 100;
            var pageNumber = request.PageNumber ?? 1;

            return new FabricAuthUserSearchResponse
            {
                HttpStatusCode =
                    fabricIdentityUserResponse != null && fabricIdentityUserResponse.HttpStatusCode != System.Net.HttpStatusCode.OK
                        ? Nancy.HttpStatusCode.PartialContent
                        : Nancy.HttpStatusCode.OK,

                Results = searchResults
                    .Filter(request)
                    .Sort(request)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
            };
        }
    }
}
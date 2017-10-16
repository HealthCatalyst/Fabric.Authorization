using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;
using Nancy.Extensions;
using Serilog;

namespace Fabric.Authorization.API.Services
{
    public class IdentitySearchService
    {
        private readonly ClientService _clientService;
        private readonly GroupService _groupService;
        private readonly IIdentityServiceProvider _identityServiceProvider;
        private readonly RoleService _roleService;
        private readonly ILogger _logger;

        public IdentitySearchService(
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

        public async Task<FabricAuthUserSearchResponse> Search(IdentitySearchRequest request)
        {
            var searchResults = new List<IdentitySearchResponse>();

            if (string.IsNullOrWhiteSpace(request.ClientId))
            {
                throw new BadRequestException<IdentitySearchRequest>("Client ID is required.");
            }

            var client = await _clientService.GetClient(request.ClientId);
            var clientRoles = await _roleService.GetRoles(client);

            var clientRoleEntities = clientRoles.ToList();
            _logger.Debug($"clientRoles = {clientRoleEntities.ListToString()}");
            if (clientRoleEntities.Count == 0)
            {
                return new FabricAuthUserSearchResponse
                {
                    HttpStatusCode = Nancy.HttpStatusCode.OK,
                    Results = new List<IdentitySearchResponse>()
                };
            }

            // get all groups tied to clientRoles
            var groupIds = clientRoleEntities.SelectMany(r => r.Groups).Distinct().ToList();
            _logger.Debug($"groupIds = {groupIds.ListToString()}");

            if (groupIds.Count == 0)
            {
                _logger.Debug("groupIds is empty - returning empty result set.");
                return new FabricAuthUserSearchResponse
                {
                    HttpStatusCode = Nancy.HttpStatusCode.OK,
                    Results = new List<IdentitySearchResponse>()
                };
            }

            var groupEntities = new List<Group>();
            foreach (var groupId in groupIds)
            {
                _logger.Debug($"getting groupId {groupId}");
                var group = await _groupService.GetGroup(groupId, request.ClientId);
                groupEntities.Add(group);
            }

            _logger.Debug($"groupEntities = {groupEntities.ListToString()}");

            var groupsMappedToClientRoles = groupEntities.Where(g => g.Roles.Any(r => clientRoleEntities.Contains(r))).ToList();
            var nonCustomGroups =
                groupsMappedToClientRoles.Where(g => !string.Equals(g.Source, GroupConstants.CustomSource, StringComparison.OrdinalIgnoreCase)).ToList();

            _logger.Debug($"nonCustomGroups = {nonCustomGroups.ListToString()}");

            // add all non-custom groups to the response
            searchResults.AddRange(nonCustomGroups.Select(g => new IdentitySearchResponse
            {
                GroupName = g.Name,
                Roles = g.Roles.Select(r => r.Name),
                EntityType = IdentitySearchResponseEntityType.Group.ToString()
            }));

            // get all users mapped to groups in client roles
            var users = groupsMappedToClientRoles
                .Where(g => g.Users != null && g.Users.Count > 0)
                .SelectMany(g => g.Users)
                .DistinctBy(u => u.SubjectId);

            var userList = new List<IdentitySearchResponse>();

            foreach (var user in users)
            {
                // get groups for user
                var userGroups = user.Groups;
                var userGroupEntities = groupEntities.Where(g => userGroups.Contains(g.Name, StringComparer.OrdinalIgnoreCase));

                // get roles for user
                var userRoles = userGroupEntities.SelectMany(g => g.Roles).Select(r => r.Name);

                // add user to response
                userList.Add(new IdentitySearchResponse
                {
                    SubjectId = user.SubjectId,
                    IdentityProvider = user.IdentityProvider,
                    Roles = userRoles,
                    EntityType = IdentitySearchResponseEntityType.User.ToString()
                });
            }

            var fabricIdentityUserResponse =
                await _identityServiceProvider.Search(request.ClientId, userList.Select(u => $"{u.SubjectId}:{u.IdentityProvider}"));

            if (fabricIdentityUserResponse != null && fabricIdentityUserResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.Debug("Populating fields from Fabric.Identity");

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

            _logger.Debug($"searchResults = {searchResults.ListToString()}");

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
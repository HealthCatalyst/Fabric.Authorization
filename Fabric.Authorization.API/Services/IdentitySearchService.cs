using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;
using Nancy.Extensions;

namespace Fabric.Authorization.API.Services
{
    public class IdentitySearchService
    {
        private readonly ClientService _clientService;
        private readonly GroupService _groupService;
        private readonly IIdentityServiceProvider _identityServiceProvider;
        private readonly RoleService _roleService;

        public IdentitySearchService(
            ClientService clientService,
            RoleService roleService,
            GroupService groupService,
            IIdentityServiceProvider identityServiceProvider)
        {
            _clientService = clientService;
            _roleService = roleService;
            _groupService = groupService;
            _identityServiceProvider = identityServiceProvider;
        }

        public async Task<IEnumerable<IdentitySearchResponse>> Search(IdentitySearchRequest request)
        {
            var searchResults = new List<IdentitySearchResponse>();

            if (string.IsNullOrWhiteSpace(request.ClientId))
            {
                throw new BadRequestException<IdentitySearchRequest>("Client ID is required.");
            }

            var client = await _clientService.GetClient(request.ClientId);
            var clientRoles = _roleService.GetRoles(client).Result.ToList();

            if (clientRoles.Count == 0)
            {
                return new List<IdentitySearchResponse>();
            }

            // get all groups tied to clientRoles
            var groupIds = clientRoles.SelectMany(r => r.Groups).Distinct().ToList();

            if (groupIds.Count == 0)
            {
                return new List<IdentitySearchResponse>();
            }

            var groupEntities = new List<Group>();
            foreach (var groupId in groupIds)
            {
                var group = await _groupService.GetGroup(groupId);
                groupEntities.Add(group);
            }

            var groupsMappedToClientRole = groupEntities.Where(g => g.Roles.Any(r => clientRoles.Contains(r))).ToList();
            var nonCustomGroups =
                groupsMappedToClientRole.Where(g => !string.Equals(g.Source, GroupConstants.CustomSource));

            // add all non-custom groups to the response
            searchResults.AddRange(nonCustomGroups.Select(g => new IdentitySearchResponse
            {
                GroupName = g.Name,
                Roles = g.Roles.Select(r => r.Name)
            }));

            // get all users mapped to groups in this role
            var users = groupsMappedToClientRole
                .Where(g => g.Users != null && g.Users.Count > 0)
                .SelectMany(g => g.Users)
                .DistinctBy(u => u.SubjectId);

            var userList = new List<IdentitySearchResponse>();

            foreach (var user in users)
            {
                // get groups for user
                var userGroups = user.Groups;
                var userGroupEntities = groupEntities.Where(g => userGroups.Contains(g.Name));

                // get roles for user
                var userRoles = userGroupEntities.SelectMany(g => g.Roles).Select(r => r.Name);

                // add user to response
                userList.Add(new IdentitySearchResponse
                {
                    SubjectId = user.SubjectId,
                    Roles = userRoles
                });
            }

            var userDetails =
                await _identityServiceProvider.Search(request.ClientId, userList.Select(u => u.SubjectId));

            if (userDetails != null)
            {
                // update user details with Fabric.Identity response
                foreach (var user in userDetails)
                {
                    var userSearchResponse = userList.FirstOrDefault(u => u.SubjectId == user.SubjectId);
                    if (userSearchResponse == null)
                    {
                        continue;
                    }

                    userSearchResponse.FirstName = user.FirstName;
                    userSearchResponse.MiddleName = user.MiddleName;
                    userSearchResponse.LastName = user.LastName;
                    userSearchResponse.LastLogin = user.LastLoginDate;
                }
            }

            searchResults.AddRange(userList);

            var pageSize = request.PageSize ?? 100;
            var pageNumber = request.PageNumber ?? 1;

            return searchResults
                .Filter(request)
                .Sort(request)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }
    }
}
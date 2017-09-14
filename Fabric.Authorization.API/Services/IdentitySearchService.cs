using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.API.Services.External.Identity;
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
            var client = await _clientService.GetClient(request.ClientId);
            var roles = await _roleService.GetRoles();
            var clientRoles = new List<Role>();

            foreach (var role in roles)
            {
                if (_clientService.DoesClientOwnItem(client.TopLevelSecurableItem, role.Grain, role.SecurableItem))
                {
                    clientRoles.Add(role);
                }
            }

            // get all groups tied to clientRoles
            var groups = await _groupService.GetAllGroups();
            var groupList = groups.ToList();

            // TODO: ensure Role equality works
            var groupsMappedToClientRole = groupList.Where(g => g.Roles.Any(r => clientRoles.Contains(r))).ToList();
            var nonCustomGroups =
                groupsMappedToClientRole.Where(g => !string.Equals(g.Source, GroupConstants.CustomSource));

            // add all non-custom groups to the response
            searchResults.AddRange(nonCustomGroups.Select(g => new IdentitySearchResponse
            {
                GroupName = g.Name,
                Roles = g.Roles.Select(r => r.Name)
            }));

            // get all users mapped to custom groups
            var users = groupsMappedToClientRole
                .Where(r => r.Users != null && r.Users.Count > 0)
                .SelectMany(r => r.Users)
                .DistinctBy(u => u.SubjectId);

            var userList = new List<IdentitySearchResponse>();

            foreach (var user in users)
            {
                // get groups for user
                var userGroups = user.Groups;
                var userGroupEntities = groupList.Where(g => userGroups.Contains(g.Name));

                // get roles for user
                var userRoles = userGroupEntities.SelectMany(g => g.Roles).Select(r => r.Name);

                // add user to response
                userList.Add(new IdentitySearchResponse
                {
                    SubjectId = user.SubjectId,
                    Roles = userRoles
                });
            }

            var userDetails = await _identityServiceProvider.Search(userList.Select(u => u.SubjectId));

            // update user details
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

            // TODO: incorporate sort key and direction
            return searchResults
                .Skip(request.PageNumber * request.PageSize)
                .Take(request.PageSize);
        }
    }
}
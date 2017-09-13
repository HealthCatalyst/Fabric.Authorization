using System;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.Domain.Stores.Services;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using Fabric.Platform.Http;

namespace Fabric.Authorization.API.Services
{
    public class IdentitySearchService
    {
        private readonly ClientService _clientService;
        private readonly RoleService _roleService;
        private readonly GroupService _groupService;
        private readonly UserService _userService;
        private readonly IHttpClientFactory _httpClientFactory;

        public IdentitySearchService(
            ClientService clientService,
            RoleService roleService,
            GroupService groupService,
            UserService userService,
            IHttpClientFactory httpClientFactory)
        {
            _clientService = clientService;
            _roleService = roleService;
            _groupService = groupService;
            _userService = userService;
            _httpClientFactory = httpClientFactory;
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
            var nonCustomGroups = groupsMappedToClientRole.Where(g => !string.Equals(g.Source, GroupConstants.CustomSource));

            // get all users mapped to custom groups
            var customGroups = groupsMappedToClientRole.Where(g => !string.Equals(g.Source, GroupConstants.CustomSource));

            // add all non-custom groups to the response
            searchResults.AddRange(nonCustomGroups.Select(g => new IdentitySearchResponse
            {
                GroupName = g.Name,
                Roles = g.Roles.Select(r => r.Name)
            }));

            // get all users in groups tied to clientRoles
            var httpClient = await _httpClientFactory.Create(new Uri("http://localhost:5001"), "fabric/identity.read");
            var response = await httpClient.GetAsync("/users");

            return searchResults;
        }
    }
}
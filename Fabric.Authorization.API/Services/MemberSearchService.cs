using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Fabric.Authorization.Models.Enums;
using Catalyst.Fabric.Authorization.Models.Search;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Serilog;

namespace Fabric.Authorization.API.Services
{
    public class MemberSearchService
    {
        private readonly ClientService _clientService;
        private readonly IIdentityServiceProvider _identityServiceProvider;
        private readonly RoleService _roleService;
        private readonly ILogger _logger;

        public MemberSearchService(
            ClientService clientService,
            RoleService roleService,
            IIdentityServiceProvider identityServiceProvider,
            ILogger logger)
        {
            _clientService = clientService;
            _roleService = roleService;
            _identityServiceProvider = identityServiceProvider;
            _logger = logger;
        }

        public async Task<FabricAuthUserSearchResponse> Search(MemberSearchRequest request)
        {
            var searchResults = new List<MemberSearchResponse>();

            var rolesToSearch = !string.IsNullOrEmpty(request.ClientId)
                ? await _roleService.GetRoles(await _clientService.GetClient(request.ClientId))
                : await _roleService.GetRoles(request.Grain, request.SecurableItem);

            var roleEntities = rolesToSearch.ToList();
            _logger.Debug($"roleEntities = {string.Join(Environment.NewLine, roleEntities)}");
            if (roleEntities.Count == 0)
            {
                return new FabricAuthUserSearchResponse
                {
                    HttpStatusCode = HttpStatusCode.OK,
                    Results = new List<MemberSearchResponse>()
                };
            }

            var groupEntities = roleEntities.SelectMany(r => r.Groups).Distinct().ToList();
            _logger.Debug($"groupEntities =  {string.Join(Environment.NewLine, groupEntities)}");

            // add groups to the response
            searchResults.AddRange(groupEntities.Select(g => new MemberSearchResponse
            {
                SubjectId = g.Name,
                GroupName = g.Name,
                Roles = g.Roles
                    .Where(r => string.IsNullOrWhiteSpace(request.Grain) || string.Equals(request.Grain, r.Grain, StringComparison.OrdinalIgnoreCase))
                    .Where(r => string.IsNullOrWhiteSpace(request.SecurableItem) || string.Equals(request.SecurableItem, r.SecurableItem, StringComparison.OrdinalIgnoreCase))
                    .Select(r => r.ToRoleApiModel()).ToList(),
                EntityType = string.Equals(g.Source, GroupConstants.CustomSource, StringComparison.OrdinalIgnoreCase)
                    ? MemberSearchResponseEntityType.CustomGroup.ToString()
                    : MemberSearchResponseEntityType.DirectoryGroup.ToString()
            }));

            // get users directly mapped to client roles
            var users = roleEntities.SelectMany(r => r.Users).Distinct(new UserComparer());
            var userList = new List<MemberSearchResponse>();

            foreach (var user in users)
            {
                // add user to response
                userList.Add(new MemberSearchResponse
                {
                    SubjectId = user.SubjectId,
                    IdentityProvider = user.IdentityProvider,
                    Roles = user.Roles.Intersect(roleEntities).Select(r => r.ToRoleApiModel()).ToList(),
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

            _logger.Debug($"searchResults =  {string.Join(Environment.NewLine, searchResults)}");

            var pageSize = request.PageSize ?? 100;
            var pageNumber = request.PageNumber ?? 1;
            var filteredResults = searchResults.Filter(request).ToList();

            return new FabricAuthUserSearchResponse
            {
                HttpStatusCode =
                    fabricIdentityUserResponse != null && fabricIdentityUserResponse.HttpStatusCode != System.Net.HttpStatusCode.OK
                        ? HttpStatusCode.PartialContent
                        : HttpStatusCode.OK,

                Results = filteredResults
                    .Sort(request)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize),

                TotalCount = filteredResults.Count
            };
        }
    }
}
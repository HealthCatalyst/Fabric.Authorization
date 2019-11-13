using System.Threading.Tasks;
using Fabric.Authorization.API.RemoteServices.Identity.Models;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;

namespace Fabric.Authorization.API.Services
{
    public class GroupSearchService
    {
        private readonly IIdentityServiceProvider _identitySearchProvider;

        public GroupSearchService(IIdentityServiceProvider identitySearchProvider)
        {
            _identitySearchProvider = identitySearchProvider;
        }

        public async Task<FabricIdentityGroupResponse> GetGroupAsync(string identityProvider, string groupName, string tenantId = null)
        {
            var result = await _identitySearchProvider.SearchGroupAsync(new GroupSearchRequest
            {
                IdentityProvider = identityProvider,
                TenantId = tenantId,
                DisplayName = groupName
            });

            return result;
        }
    }
}

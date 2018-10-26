using System.Threading.Tasks;
using Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Models;
using Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Providers;

namespace Fabric.Authorization.API.Services
{
    public class IdPSearchService
    {
        private readonly IIdPSearchProvider _idPSearchProvider;

        public IdPSearchService(IIdPSearchProvider idPSearchProvider)
        {
            _idPSearchProvider = idPSearchProvider;
        }

        public async Task<FabricIdPGroupResponse> GetGroupAsync(string identityProvider, string groupName, string tenantId = null)
        {
            var result = await _idPSearchProvider.GetGroupAsync(new IdPGroupRequest
            {
                IdentityProvider = identityProvider,
                TenantId = tenantId,
                DisplayName = groupName
            });

            return result;
        }
    }
}

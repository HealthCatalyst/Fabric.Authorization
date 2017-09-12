using System.Threading.Tasks;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.Domain.Stores.Services;

namespace Fabric.Authorization.API.Services
{
    public class IdentitySearchService
    {
        private readonly ClientService _clientService;

        public IdentitySearchService(ClientService clientService)
        {
            _clientService = clientService;
        }

        public async Task<IdentitySearchResponse> Search(IdentitySearchRequest request)
        {
            var client = await _clientService.GetClient(request.ClientId);

            return new IdentitySearchResponse();
        }
    }
}
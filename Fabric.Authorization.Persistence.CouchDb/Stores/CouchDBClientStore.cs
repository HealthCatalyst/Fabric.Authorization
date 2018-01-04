using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.CouchDb.Services;
using Serilog;

namespace Fabric.Authorization.Persistence.CouchDb.Stores
{
    public class CouchDbClientStore : CouchDbGenericStore<string, Client>, IClientStore
    {
        public CouchDbClientStore(IDocumentDbService dbService, ILogger logger,
            IEventContextResolverService eventContextResolverService) : base(dbService, logger,
            eventContextResolverService)
        {
        }

        public override async Task<Client> Add(Client client)
        {
            return await Add(client.Id, client);
        }

        public override async Task Delete(Client client)
        {
            await Delete(client.Id, client);
        }
    }
}
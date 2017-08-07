using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public class CouchDbClientStore : CouchDbGenericStore<string, Client>, IClientStore
    {
        public CouchDbClientStore(IDocumentDbService dbService, ILogger logger, IEventContextResolverService eventContextResolverService) : base(dbService, logger, eventContextResolverService)
        {
        }

        public override async Task<Client> Add(Client client) => await this.Add(client.Id, client);

        public override async Task Delete(Client client) => await this.Delete(client.Id, client);
    }
}
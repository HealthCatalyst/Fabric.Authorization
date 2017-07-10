using System;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores
{
    public class CouchDBClientStore : CouchDBGenericStore<string, Client>, IClientStore
    {
        public CouchDBClientStore(IDocumentDbService dbService, ILogger logger) : base(dbService, logger)
        {
        }

        public override Client Add(Client client) => this.Add(client.Id, client);

        public override void Delete(Client client) => this.Delete(client.Id, client);

    }
}
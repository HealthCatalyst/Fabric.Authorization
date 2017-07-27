﻿using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public class CouchDBClientStore : CouchDBGenericStore<string, Client>, IClientStore
    {
        public CouchDBClientStore(IDocumentDbService dbService, ILogger logger) : base(dbService, logger)
        {
        }

        public override async Task<Client> Add(Client client) => await this.Add(client.Id, client);

        public override async Task Delete(Client client) => await this.Delete(client.Id, client);
    }
}
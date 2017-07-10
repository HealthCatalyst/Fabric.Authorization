using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores
{
    public class CouchDBGroupStore : CouchDBGenericStore<string, Group>, IGroupStore
    {
        public CouchDBGroupStore(IDocumentDbService dbService, ILogger logger) : base(dbService, logger)
        {
        }

        public override Group Add(Group group) => this.Add(group.Id, group);

        public override void Delete(Group group) => this.Delete(group.Id, group);

        public override IEnumerable<Group> GetAll()
        {
            return _dbService.GetDocuments<Group>("group").Result;
        }
    }
}
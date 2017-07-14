using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores
{
    public class CouchDBGroupStore : CouchDBGenericStore<string, Group>, IGroupStore
    {
        public CouchDBGroupStore(IDocumentDbService dbService, ILogger logger) : base(dbService, logger)
        {
        }

        public override async Task<Group> Add(Group group) => await this.Add(group.Id, group);

        public override async Task Delete(Group group) => await this.Delete(group.Id, group);

        public override async Task<IEnumerable<Group>> GetAll()
        {
            return await _dbService.GetDocuments<Group>("group");
        }
    }
}
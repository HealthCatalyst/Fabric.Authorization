using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public class CouchDbGroupStore : FormattableIdentifierStore<string, Group>, IGroupStore
    {
        public CouchDbGroupStore(
            IDocumentDbService dbService,
            ILogger logger,
            IEventContextResolverService eventContextResolverService,
            IIdentifierFormatter identifierFormatter) : base(dbService, logger, eventContextResolverService, identifierFormatter)
        {
        }

        public override async Task<Group> Get(string id)
        {
            return await base.Get(FormatId(id));
        }

        public override async Task<bool> Exists(string id)
        {
            return await base.Exists(FormatId(id));
        }

        public override async Task<Group> Add(Group group)
        {
            return await Add(FormatId(group.Id), group).ConfigureAwait(false);
        }

        protected override async Task Update(string id, Group model)
        {
            model.Track(false, GetActor());
            await ExponentialBackoff(_dbService.UpdateDocument(FormatId(model.Identifier), model)).ConfigureAwait(false);
        }

        public override async Task Delete(Group group)
        {
            await Delete(FormatId(group.Id), group).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<Group>> GetAll()
        {
            return await _dbService.GetDocuments<Group>("group");
        }
    }
}
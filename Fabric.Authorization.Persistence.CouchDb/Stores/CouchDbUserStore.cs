using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.CouchDb.Services;
using Serilog;

namespace Fabric.Authorization.Persistence.CouchDb.Stores
{
    public class CouchDbUserStore : FormattableIdentifierStore<string, User>, IUserStore
    {
        public CouchDbUserStore(
            IDocumentDbService dbService,
            ILogger logger,
            IEventContextResolverService eventContextResolverService,
            IIdentifierFormatter identifierFormatter) : base(dbService, logger, eventContextResolverService, identifierFormatter)
        {
        }

        public override async Task<User> Get(string id)
        {
            return await base.Get(FormatId(id));
        }

        public override async Task<bool> Exists(string id)
        {
            return await base.Exists(FormatId(id));
        }

        public Task<User> AddRolesToUser(User user, IList<Role> roles)
        {
            throw new System.NotImplementedException();
        }

        public override async Task<User> Add(User model)
        {
            return await base.Add(FormatId(model.Id), model).ConfigureAwait(false);
        }

        protected override async Task Update(string id, User model)
        {
            model.Track(false, GetActor());
            await ExponentialBackoff(DocumentDbService.UpdateDocument(FormatId(model.Identifier), model)).ConfigureAwait(false);
        }

        public override async Task Delete(User model)
        {
            await base.Delete(FormatId(model.Id), model).ConfigureAwait(false);
        }
    }
}
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public class CouchDbUserStore : CouchDbGenericStore<string, User>, IUserStore, IThirdPartyIdentifier
    {
        private readonly IdpIdentifierFormatter _idpIdentifierFormatter = new IdpIdentifierFormatter();

        public CouchDbUserStore(
            IDocumentDbService dbService,
            ILogger logger,
            IEventContextResolverService eventContextResolverService) : base(dbService, logger, eventContextResolverService)
        {
        }

        public string FormatId(string id)
        {
            return _idpIdentifierFormatter.Format(id);
        }

        public override async Task<User> Get(string id)
        {
            return await base.Get(FormatId(id));
        }

        public override async Task<bool> Exists(string id)
        {
            return await base.Exists(FormatId(id));
        }

        public override async Task<User> Add(User model)
        {
            return await base.Add(FormatId(model.Id), model);
        }

        protected override async Task Update(string id, User model)
        {
            model.Track(false, GetActor());
            await ExponentialBackoff(_dbService.UpdateDocument(FormatId(model.Identifier), model)).ConfigureAwait(false);
        }

        public override async Task Delete(User model)
        {
            await base.Delete(FormatId(model.Id), model);
        }
    }
}
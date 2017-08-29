using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public class CouchDbUserStore : CouchDbGenericStore<string, User>, IUserStore
    {
        public CouchDbUserStore(IDocumentDbService dbService, ILogger logger,
            IEventContextResolverService eventContextResolverService) : base(dbService, logger,
            eventContextResolverService)
        {
        }

        public override async Task<User> Add(User model)
        {
            model.Id = model.SubjectId;
            return await base.Add(model.Id, model);
        }

        public override async Task Delete(User model)
        {
            await base.Delete(model.Id, model);
        }
    }
}
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
            return await base.Add(ReplaceInvalidChars(model.Identifier), model);
        }

        public override async Task Delete(User model)
        {
            await base.Delete(ReplaceInvalidChars(model.Identifier), model);
        }

        public static string ReplaceInvalidChars(string documentId)
        {
            return documentId.Replace(@"\", "::");
        }
    }
}
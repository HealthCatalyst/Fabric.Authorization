using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public abstract class FormattableIdentifierStore<K, T> : CouchDbGenericStore<K, T>, IFormattableIdentifier
        where T : ITrackable, IIdentifiable
    {
        protected readonly IIdentifierFormatter IdentifierFormatter;

        protected FormattableIdentifierStore(
            IDocumentDbService dbService,
            ILogger logger,
            IEventContextResolverService eventContextResolverService,
            IIdentifierFormatter identifierFormatter
            ) : base(dbService, logger, eventContextResolverService)
        {
            IdentifierFormatter = identifierFormatter;
        }

        public string FormatId(string id)
        {
            return IdentifierFormatter.Format(id);
        }
    }
}
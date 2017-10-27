using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.InMemory
{
    public abstract class InMemoryFormattableIdentifierStore<T> : InMemoryGenericStore<T>
        where T : ITrackable, IIdentifiable, ISoftDelete
    {
        protected readonly IIdentifierFormatter IdentifierFormatter;

        protected InMemoryFormattableIdentifierStore(IIdentifierFormatter identifierFormatter)
        {
            IdentifierFormatter = identifierFormatter;
        }

        public string FormatId(string id)
        {
            return IdentifierFormatter.Format(id);
        }

        public override async Task<T> Get(string id)
        {
            return await base.Get(FormatId(id));
        }

        public override async Task<T> Add(T model)
        {
            model.Track();

            var formattedId = FormatId(model.Identifier);

            if (await Exists(formattedId).ConfigureAwait(false))
            {
                throw new AlreadyExistsException<T>(model, model.Identifier);
            }

            Dictionary.TryAdd(formattedId, model);
            return model;
        }

        public override async Task Update(T model)
        {
            model.Track(false);

            var formattedId = FormatId(model.Identifier);

            if (await Exists(formattedId).ConfigureAwait(false))
            {
                if (!Dictionary.TryUpdate(formattedId, model, Dictionary[formattedId]))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new NotFoundException<T>(model, model.Identifier);
            }
        }

        public override Task<bool> Exists(string id)
        {
            return base.Exists(FormatId(id));
        }
    }
}
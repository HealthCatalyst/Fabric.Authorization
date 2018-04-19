using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using SecurableItem = Fabric.Authorization.Persistence.SqlServer.EntityModels.SecurableItem;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerClientStore : SqlServerBaseStore, IClientStore
    {
        private readonly IGrainStore _grainStore;

        public SqlServerClientStore(IAuthorizationDbContext authorizationDbContext, IEventService eventService, IGrainStore grainStore) :
            base(authorizationDbContext, eventService)
        {
            _grainStore = grainStore;
        }

        public async Task<Client> Add(Client model)
        {
            Grain grain = null;
            if (model.TopLevelSecurableItem != null)
            {
                grain = await _grainStore.Get(model.TopLevelSecurableItem.Grain);
            }

            var clientEntity = model.ToEntity();
            clientEntity.TopLevelSecurableItem.GrainId = grain?.Id;

            AuthorizationDbContext.Clients.Add(clientEntity);
            await AuthorizationDbContext.SaveChangesAsync();
            return model;
        }

        /// <summary>
        /// </summary>
        /// <param name="id">This is the unique client id of the client</param>
        /// <returns></returns>
        public async Task<Client> Get(string id)
        {
            var client = await AuthorizationDbContext.Clients
                .Include(i => i.TopLevelSecurableItem)
                .SingleOrDefaultAsync(c => c.ClientId == id
                                           && !c.IsDeleted);

            if (client == null)
            {
                throw new NotFoundException<Client>($"Could not find {typeof(Client).Name} entity with ID {id}");
            }

            return client.ToModel();
        }

        public async Task<IEnumerable<Client>> GetAll()
        {
            var clients = await AuthorizationDbContext.Clients
                .Where(c => !c.IsDeleted)
                .ToArrayAsync();

            return clients.Select(c => c.ToModel());
        }

        public async Task Delete(Client model)
        {
            var client = await AuthorizationDbContext.Clients
                .Include(i => i.TopLevelSecurableItem)
                .SingleOrDefaultAsync(c => c.ClientId == model.Id
                                           && !c.IsDeleted);

            if (client == null)
            {
                throw new NotFoundException<Client>($"Could not find {typeof(Client).Name} entity with ID {model.Id}");
            }

            client.IsDeleted = true;
            MarkSecurableItemsDeleted(client.TopLevelSecurableItem);

            await AuthorizationDbContext.SaveChangesAsync();
        }

        public async Task Update(Client model)
        {
            var client = await AuthorizationDbContext.Clients
                .Include(i => i.TopLevelSecurableItem)
                .SingleOrDefaultAsync(c => c.ClientId == model.Id
                                           && !c.IsDeleted);
            if (client == null)
            {
                throw new NotFoundException<Client>($"Could not find {typeof(Client).Name} entity with ID {model.Id}");
            }

            model.ToEntity(client);

            AuthorizationDbContext.Clients.Update(client);
            await AuthorizationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// </summary>
        /// <param name="id">This is the unique client id of the client</param>
        /// <returns></returns>
        public async Task<bool> Exists(string id)
        {
            var client = await AuthorizationDbContext.Clients
                .SingleOrDefaultAsync(c => c.ClientId == id
                                           && !c.IsDeleted).ConfigureAwait(false);

            return client != null;
        }

        private void MarkSecurableItemsDeleted(SecurableItem topLevelSecurableItem)
        {
            topLevelSecurableItem.IsDeleted = true;
            foreach (var securableItem in topLevelSecurableItem.SecurableItems)
            {
                MarkSecurableItemsDeleted(securableItem);
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Events;
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

        public async Task<Client> Add(Client client)
        {
            Grain grain = null;
            if (client.TopLevelSecurableItem != null)
            {
                grain = await _grainStore.Get(client.TopLevelSecurableItem.Grain);
            }

            var clientEntity = client.ToEntity();
            clientEntity.TopLevelSecurableItem.GrainId = grain?.Id;

            AuthorizationDbContext.Clients.Add(clientEntity);
            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Client>(EventTypes.EntityCreatedEvent, client.Id, client));
            return client;
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

        public async Task Delete(Client client)
        {
            var clientEntity = await AuthorizationDbContext.Clients
                .Include(i => i.TopLevelSecurableItem)
                .SingleOrDefaultAsync(c => c.ClientId == client.Id
                                           && !c.IsDeleted);

            if (clientEntity == null)
            {
                throw new NotFoundException<Client>($"Could not find {typeof(Client).Name} entity with ID {client.Id}");
            }

            client.IsDeleted = true;
            MarkSecurableItemsDeleted(clientEntity.TopLevelSecurableItem);

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Client>(EventTypes.EntityDeletedEvent, client.Id, client));
        }

        public async Task Update(Client client)
        {
            var clientEntity = await AuthorizationDbContext.Clients
                .Include(i => i.TopLevelSecurableItem)
                .SingleOrDefaultAsync(c => c.ClientId == client.Id
                                           && !c.IsDeleted);
            if (clientEntity == null)
            {
                throw new NotFoundException<Client>($"Could not find {typeof(Client).Name} entity with ID {client.Id}");
            }

            client.ToEntity(clientEntity);

            AuthorizationDbContext.Clients.Update(clientEntity);
            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Client>(EventTypes.EntityUpdatedEvent, client.Id, client));
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

        private static void MarkSecurableItemsDeleted(SecurableItem topLevelSecurableItem)
        {
            topLevelSecurableItem.IsDeleted = true;
            foreach (var securableItem in topLevelSecurableItem.SecurableItems)
            {
                MarkSecurableItemsDeleted(securableItem);
            }
        }
    }
}
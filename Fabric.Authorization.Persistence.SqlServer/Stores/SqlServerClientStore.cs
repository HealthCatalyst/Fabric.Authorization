using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerClientStore : IClientStore
    {
        private readonly IAuthorizationDbContext _authorizationDbContext;

        public SqlServerClientStore(IAuthorizationDbContext authorizationDbContext)
        {
            _authorizationDbContext = authorizationDbContext;
        }

        public async Task<Client> Add(Client model)
        {
            var clientEntity = model.ToEntity();

            _authorizationDbContext.Clients.Add(clientEntity);
            await _authorizationDbContext.SaveChangesAsync();
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">This is the unique client id of the client</param>
        /// <returns></returns>
        public async Task<Client> Get(string id)
        {
            var client = await _authorizationDbContext.Clients
                .Include(i => i.TopLevelSecurableItem)
                .SingleOrDefaultAsync(c => c.ClientId.Equals(id, StringComparison.OrdinalIgnoreCase)
                                           && !c.IsDeleted);

            if (client == null)
            {
                throw new NotFoundException<Client>($"Could not find {typeof(Client).Name} entity with ID {id}");
            }

            return client.ToModel();
        }

        public async Task<IEnumerable<Client>> GetAll()
        {
            var clients = await _authorizationDbContext.Clients
                .Where(c => !c.IsDeleted)
                .ToArrayAsync();

            return clients.Select(c => c.ToModel());
        }

        public async Task Delete(Client model)
        {
            var client = await _authorizationDbContext.Clients
                .Include(i => i.TopLevelSecurableItem)
                .SingleOrDefaultAsync(c => c.ClientId.Equals(model.Id, StringComparison.OrdinalIgnoreCase)
                                           && !c.IsDeleted);

            client.IsDeleted = true;
            client.TopLevelSecurableItem.IsDeleted = true;

            foreach (var securableItem in client.TopLevelSecurableItem.SecurableItems)
            {
                securableItem.IsDeleted = true;
            }

            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task Update(Client model)
        {
            var client = await _authorizationDbContext.Clients
                .Include(i => i.TopLevelSecurableItem)
                .SingleOrDefaultAsync(c => c.ClientId.Equals(model.Id, StringComparison.OrdinalIgnoreCase)
                                           && !c.IsDeleted);

            model.ToEntity(client);

            _authorizationDbContext.Clients.Update(client);
            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task<bool> Exists(string id)
        {
            var client = await _authorizationDbContext.Clients
                .Include(i => i.TopLevelSecurableItem)
                .SingleOrDefaultAsync(c => c.ClientId.Equals(id, StringComparison.OrdinalIgnoreCase)
                                           && !c.IsDeleted);

            return client != null;
        }
    }
}

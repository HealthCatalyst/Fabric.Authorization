using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerSecurableItemStore : SqlServerBaseStore, ISecurableItemStore
    {
        public SqlServerSecurableItemStore(IAuthorizationDbContext authorizationDbContext, IEventService eventService) :
            base(authorizationDbContext, eventService)
        {
        }

        public async Task<SecurableItem> Get(string name)
        {
            var securableItem = await AuthorizationDbContext.SecurableItems
                .Include(s => s.Grain)
                .Include(s => s.SecurableItems)
                .SingleOrDefaultAsync(s => s.Name == name && !s.IsDeleted);

            if (securableItem == null)
            {
                throw new NotFoundException<SecurableItem>();
            }

            return securableItem.ToModel();
        }

        public async Task<SecurableItem> Get(Guid id)
        {
            var securableItem = await AuthorizationDbContext.SecurableItems
                .Include(s => s.Grain)
                .Include(s => s.SecurableItems)
                .SingleOrDefaultAsync(s => s.SecurableItemId == id && !s.IsDeleted);

            if (securableItem == null)
            {
                throw new NotFoundException<SecurableItem>();
            }

            return securableItem.ToModel();
        }
    }
}
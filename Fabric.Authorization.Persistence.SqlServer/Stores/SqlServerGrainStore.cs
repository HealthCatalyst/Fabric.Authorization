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
    public class SqlServerGrainStore : SqlServerBaseStore, IGrainStore
    {
        public SqlServerGrainStore(IAuthorizationDbContext authorizationDbContext, IEventService eventService) :
            base(authorizationDbContext, eventService)
        {
        }

        public async Task<Grain> Get(string name)
        {
            var grain = await AuthorizationDbContext.Grains
                .Include(g => g.SecurableItems)
                .SingleOrDefaultAsync(g => g.Name == name && !g.IsDeleted).ConfigureAwait(false);

            if (grain == null)
            {
                throw new NotFoundException<Grain>($"Could not find {typeof(Grain).Name} entity with Name {name}");
            }

            foreach (var securableItem in grain.SecurableItems)
            {
                LoadChildrenRecursive(securableItem);
            }

            return grain.ToModel();
        }

        public Task<IEnumerable<Grain>> GetSharedGrains()
        {
            var sharedGrains = AuthorizationDbContext.Grains
                .Include(g => g.SecurableItems)
                .Where(g => !g.IsDeleted && g.IsShared)
                .Select(g => g.ToModel())
                .AsEnumerable();

            return Task.FromResult(sharedGrains);
        }

        private void LoadChildrenRecursive(SecurableItem securableItem)
        {
            AuthorizationDbContext.Entry(securableItem)
                .Collection(s => s.SecurableItems)
                .Load();

            foreach (var childSecurableItem in securableItem.SecurableItems)
            {
                LoadChildrenRecursive(childSecurableItem);
            }
        }
    }
}
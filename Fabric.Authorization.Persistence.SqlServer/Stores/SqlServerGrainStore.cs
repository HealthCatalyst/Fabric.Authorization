using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerGrainStore : IGrainStore
    {
        private readonly IAuthorizationDbContext _authorizationDbContext;

        public SqlServerGrainStore(IAuthorizationDbContext authorizationDbContext)
        {
            _authorizationDbContext = authorizationDbContext ??
                                      throw new ArgumentNullException(nameof(authorizationDbContext));
        }

        public async Task<Grain> Get(string name)
        {
            var grain = await _authorizationDbContext.Grains
                .Include(g => g.SecurableItems)
                .SingleOrDefaultAsync(g => g.Name == name);

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

        private void LoadChildrenRecursive(EntityModels.SecurableItem securableItem)
        {
            _authorizationDbContext.Entry(securableItem)
                .Collection(s => s.SecurableItems)
                .Load();

            foreach (var childSecurableItem in securableItem.SecurableItems)
            {
                LoadChildrenRecursive(childSecurableItem);
            }
        }
    }
}

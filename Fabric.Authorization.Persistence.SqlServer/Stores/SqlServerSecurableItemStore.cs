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
    public class SqlServerSecurableItemStore : ISecurableItemStore
    {
        private readonly IAuthorizationDbContext _authorizationDbContext;

        public SqlServerSecurableItemStore(IAuthorizationDbContext authorizationDbContext)
        {
            _authorizationDbContext = authorizationDbContext ??
                                      throw new ArgumentNullException(nameof(authorizationDbContext));
        }
        public async Task<SecurableItem> Get(string name)
        {
            var securableItem =  await _authorizationDbContext.SecurableItems.SingleOrDefaultAsync(
                s => s.Name == name && s.IsDeleted == false);
            if (securableItem == null)
            {
                throw new NotFoundException<SecurableItem>();
            }

            return securableItem.ToModel();
        }
    }
}

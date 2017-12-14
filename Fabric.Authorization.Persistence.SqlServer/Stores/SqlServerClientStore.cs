using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Services;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerClientStore : IClientStore
    {
        private readonly IAuthorizationDbContext _authorizationDbContext;

        public SqlServerClientStore(IAuthorizationDbContext authorizationDbContext)
        {
            _authorizationDbContext = authorizationDbContext;
        }

        public Task<Client> Add(Client model)
        {
            throw new NotImplementedException();
        }

        public Task<Client> Get(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Client>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task Delete(Client model)
        {
            throw new NotImplementedException();
        }

        public Task Update(Client model)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Exists(string id)
        {
            throw new NotImplementedException();
        }
    }
}

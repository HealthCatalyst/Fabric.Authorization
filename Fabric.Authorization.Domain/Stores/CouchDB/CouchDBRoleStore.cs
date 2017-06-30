using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores
{
    public class CouchDBRoleStore : CouchDBGenericStore<Guid, Role>, IRoleStore
    {
        public CouchDBRoleStore(IDocumentDbService dbService, ILogger logger) : base(dbService, logger)
        {
        }

        public override Role Add(Role model) => throw new NotImplementedException();
        public override void Delete(Role model) => throw new NotImplementedException();
        public IEnumerable<Role> GetRoles(string grain = null, string securableItem = null, string roleName = null) => throw new NotImplementedException();
        public void UpdateRole(Role role) => throw new NotImplementedException();
    }
}

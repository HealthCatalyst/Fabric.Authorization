using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores
{
    public class CouchDBPermissionStore : CouchDBGenericStore<Guid, Permission>, IPermissionStore
    {
        public CouchDBPermissionStore(IDocumentDbService dbService, ILogger logger) : base(dbService, logger)
        {
        }

        public override Permission Add(Permission model) => throw new NotImplementedException();
        public override void Delete(Permission model) => throw new NotImplementedException();
        public IEnumerable<Permission> GetPermissions(string grain = null, string securableItem = null, string permissionName = null) => throw new NotImplementedException();
        public void UpdatePermission(Permission permission) => throw new NotImplementedException();
    }
}

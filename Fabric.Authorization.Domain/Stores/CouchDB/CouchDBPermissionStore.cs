using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.API.Services;
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

        public override Permission Add(Permission model)
        {
            model.Id = Guid.NewGuid();
            return base.Add(model.Id.ToString(), model);
        }

        public override void Delete(Permission model) => base.Delete(model.Id.ToString(), model);
        public IEnumerable<Permission> GetPermissions(string grain = null, string securableItem = null, string permissionName = null)
        {
            var customParams = grain+securableItem+permissionName;
            return _dbService.GetDocuments<Permission>("permissions", "byname", customParams).Result;
        }

        protected override void AddViews()
        {
            var views = new Dictionary<string, Dictionary<string, string>>()
            {
                {
                    "byname",
                    new Dictionary<string, string>()
                    {
                        { "map", "function(doc) { return emit(doc.Grain+doc.SecurableItem+doc.Name, doc); }" },
                    }
                }
            };

            var couchViews = new CouchDBViews()
            {
                id = "permissions",
                views = views
            };

            _dbService.AddViews("permissions", couchViews);
        }
    }
}

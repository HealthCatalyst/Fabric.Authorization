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
    public class CouchDBRoleStore : CouchDBGenericStore<Guid, Role>, IRoleStore
    {
        public CouchDBRoleStore(IDocumentDbService dbService, ILogger logger) : base(dbService, logger)
        {
        }

        public override Role Add(Role model)
        {
            model.Id = Guid.NewGuid();
            return base.Add(model.Id.ToString(), model);
        }

        public override void Delete(Role model) => base.Delete(model.Id.ToString(), model);
        public IEnumerable<Role> GetRoles(string grain = null, string securableItem = null, string roleName = null)
        {
            var customParams = grain + securableItem + roleName;
            return _dbService.GetDocuments<Role>("roles", "byname", customParams).Result;
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
                id = "roles",
                views = views
            };

            _dbService.AddViews("roles", couchViews);
        }
    }
}

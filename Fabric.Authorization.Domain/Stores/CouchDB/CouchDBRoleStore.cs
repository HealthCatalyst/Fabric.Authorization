using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public class CouchDbRoleStore : CouchDbGenericStore<Guid, Role>, IRoleStore
    {
        public CouchDbRoleStore(IDocumentDbService dbService, ILogger logger) : base(dbService, logger)
        {
        }

        public override async Task<Role> Add(Role model)
        {
            model.Id = Guid.NewGuid();
            return await base.Add(model.Id.ToString(), model);
        }

        public override async Task Delete(Role model) => await base.Delete(model.Id.ToString(), model);
        
        public async Task<IEnumerable<Role>> GetRoles(string grain, string securableItem = null, string roleName = null)
        {
            var customParams = grain + securableItem + roleName;
            return roleName != null ?
                await _dbService.GetDocuments<Role>("roles", "byname", customParams) :
                await _dbService.GetDocuments<Role>("roles", "bysecitem", customParams);
        }
        
        public static CouchDbViews GetViews()
        {
            var views = new Dictionary<string, Dictionary<string, string>>()
            {
                {
                    "byname", // Stores all roles by gain+secitem+name for easy retrieval.
                    new Dictionary<string, string>()
                    {
                        { "map", "function(doc) { if (doc._id.indexOf('role:') !== -1) emit(doc.Grain+doc.SecurableItem+doc.Name, doc); }" },
                    }
                },
                {
                    "bysecitem", // Stores all roles by gain+secitem for easy retrieval.
                    new Dictionary<string, string>()
                    {
                        { "map", "function(doc) { if (doc._id.indexOf('role:') !== -1) emit(doc.Grain+doc.SecurableItem, doc); }" },
                    }
                }
            };

            var couchViews = new CouchDbViews()
            {
                id = "roles",
                views = views
            };
            return couchViews;
        }
    }
}
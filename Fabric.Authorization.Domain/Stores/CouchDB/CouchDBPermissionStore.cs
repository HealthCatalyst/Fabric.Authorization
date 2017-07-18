using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores
{
    public class CouchDBPermissionStore : CouchDBGenericStore<Guid, Permission>, IPermissionStore
    {
        public CouchDBPermissionStore(IDocumentDbService dbService, ILogger logger) : base(dbService, logger)
        {
        }

        public override async Task<Permission> Add(Permission model)
        {
            model.Id = Guid.NewGuid();
            return await base.Add(model.Id.ToString(), model);
        }

        public override async Task Delete(Permission model) => await base.Delete(model.Id.ToString(), model);

        public async Task<IEnumerable<Permission>> GetPermissions(string grain, string securableItem = null, string permissionName = null)
        {
            var customParams = grain + securableItem + permissionName;
            return permissionName != null ?
                  await _dbService.GetDocuments<Permission>("permissions", "byname", customParams) :
                  await _dbService.GetDocuments<Permission>("permissions", "bysecitem", customParams);
        }

        protected override async Task AddViews()
        {
            var views = new Dictionary<string, Dictionary<string, string>>()
            {
                {
                    "byname",
                    new Dictionary<string, string>()
                    {
                        { "map", "function(doc) { if (doc._id.indexOf('permission:') !== -1) emit(doc.Grain+doc.SecurableItem+doc.Name, doc) }" },
                    }
                },
                {
                    "bysecitem",
                    new Dictionary<string, string>()
                    {
                        { "map", "function(doc) { if (doc._id.indexOf('permission:') !== -1) emit(doc.Grain+doc.SecurableItem, doc) }" },
                    }
                }
            };

            var couchViews = new CouchDBViews()
            {
                id = "permissions",
                views = views
            };
            
            await _dbService.AddViews("permissions", couchViews);
        }
    }
}
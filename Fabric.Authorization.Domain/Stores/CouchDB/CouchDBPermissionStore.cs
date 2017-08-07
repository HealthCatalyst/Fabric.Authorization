using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public class CouchDbPermissionStore : CouchDbGenericStore<Guid, Permission>, IPermissionStore
    {
        public CouchDbPermissionStore(IDocumentDbService dbService, ILogger logger) : base(dbService, logger)
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

        public static CouchDbViews GetViews()
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

            var couchViews = new CouchDbViews()
            {
                id = "permissions",
                views = views
            };
            return couchViews;
        }

        public async Task AddOrUpdateGranularPermission(GranularPermission granularPermission)
        {
            var perm = await _dbService.GetDocument<GranularPermission>(granularPermission.Id);

            if (perm == null)
            {
                await _dbService.AddDocument(granularPermission.Id, granularPermission);
            }
            else
            {
                await _dbService.UpdateDocument(granularPermission.Id, granularPermission);
            }
        }

        public async Task<GranularPermission> GetGranularPermission(string target)
        {
            var perm = await _dbService.GetDocument<GranularPermission>(target);
            if (perm == null)
            {
                throw new NotFoundException<GranularPermission>(target);
            }

            return perm;
        }
    }
}
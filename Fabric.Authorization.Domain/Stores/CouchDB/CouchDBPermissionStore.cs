using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public class CouchDbPermissionStore : FormattableIdentifierStore<Guid, Permission>, IPermissionStore
    {
        public CouchDbPermissionStore(
            IDocumentDbService dbService,
            ILogger logger,
            IEventContextResolverService eventContextResolverService,
            IIdentifierFormatter identifierFormatter) : base(dbService, logger, eventContextResolverService, identifierFormatter)
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
                  await DocumentDbService.GetDocuments<Permission>("permissions", "byname", customParams) :
                  await DocumentDbService.GetDocuments<Permission>("permissions", "bysecitem", customParams);
        }

        public static CouchDbViews GetViews()
        {
            var views = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "byname",
                    new Dictionary<string, string>
                    {
                        { "map", "function(doc) { if (doc._id.indexOf('permission:') !== -1) emit(doc.Grain+doc.SecurableItem+doc.Name, doc) }" },
                    }
                },
                {
                    "bysecitem",
                    new Dictionary<string, string>
                    {
                        { "map", "function(doc) { if (doc._id.indexOf('permission:') !== -1) emit(doc.Grain+doc.SecurableItem, doc) }" },
                    }
                }
            };

            var couchViews = new CouchDbViews
            {
                id = "permissions",
                views = views
            };
            return couchViews;
        }

        public async Task AddOrUpdateGranularPermission(GranularPermission granularPermission)
        {
            var userId = FormatId(granularPermission.Id);
            var perm = await DocumentDbService.GetDocument<GranularPermission>(userId);

            if (perm == null)
            {
                await DocumentDbService.AddDocument(userId, granularPermission);
            }
            else
            {
                await DocumentDbService.UpdateDocument(userId, granularPermission);
            }
        }

        public async Task<GranularPermission> GetGranularPermission(string userId)
        {
            var perm = await DocumentDbService.GetDocument<GranularPermission>(FormatId(userId));
            if (perm == null)
            {
                throw new NotFoundException<GranularPermission>(userId);
            }

            return perm;
        }
    }
}
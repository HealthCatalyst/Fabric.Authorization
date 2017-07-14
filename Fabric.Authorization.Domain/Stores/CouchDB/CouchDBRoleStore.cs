using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores
{
    public class CouchDBRoleStore : CouchDBGenericStore<Guid, Role>, IRoleStore
    {
        public CouchDBRoleStore(IDocumentDbService dbService, ILogger logger) : base(dbService, logger)
        {
        }

        public override async Task<Role> Add(Role model)
        {
            model.Id = Guid.NewGuid();
            return await base.Add(model.Id.ToString(), model);
        }

        public override async Task Delete(Role model) => await base.Delete(model.Id.ToString(), model);

        public async Task<IEnumerable<Role>> GetRoleHierarchy(Guid roleId)
        {
            // TODO: Use view
            // return await _dbService.GetDocuments<Role>("roles", "resolvepermissions", roleId.ToString());

            var queue = new Queue<Guid>();
            queue.Enqueue(roleId);

            var roleHierarchy = new HashSet<Role>();

            while (queue.Any())
            {
                var topId = queue.Dequeue();
                if (await this.Exists(topId))
                {
                    var role = await this.Get(topId);
                    roleHierarchy.Add(role);
                    if (role.ParentRole.HasValue)
                    {
                        queue.Enqueue(role.ParentRole.Value);
                    }
                }
            }

            return roleHierarchy;
        }

        public async Task<IEnumerable<Role>> GetRoles(string grain, string securableItem = null, string roleName = null)
        {
            var customParams = grain + securableItem + roleName;
            return roleName != null ?
                await _dbService.GetDocuments<Role>("roles", "byname", customParams) :
                await _dbService.GetDocuments<Role>("roles", "bysecitem", customParams);
        }

        protected override void AddViews()
        {
            var views = new Dictionary<string, Dictionary<string, string>>()
            {
                {
                    "byname", // Stores all roles by gain+secitem+name for easy retrieval.
                    new Dictionary<string, string>()
                    {
                        { "map", "function(doc) { return emit(doc.Grain+doc.SecurableItem+doc.Name, doc); }" },
                    }
                },
                {
                    "bysecitem", // Stores all roles by gain+secitem for easy retrieval.
                    new Dictionary<string, string>()
                    {
                        { "map", "function(doc) { return emit(doc.Grain+doc.SecurableItem, doc); }" },
                    }
                },
                {
                    "resolvepermissions",
                    new Dictionary<string, string>()
                    {
                        { "map", @"function(doc) {
                                     var queue = [];
                                     var hierarchy = [];
                                     queue.push(doc.Id);

                                     while (queue.length > 0) {
                                         var topId = queue.shift();
                                         role = Role(topId);

                                         if (role && role.ParentRole) {
                                             queue.push(role.ParentRole);
                                         }

                                         hierarchy.push(topId);
                                     }

                                     emit(doc.Id, hierarchy);
                                 }"
                        },
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
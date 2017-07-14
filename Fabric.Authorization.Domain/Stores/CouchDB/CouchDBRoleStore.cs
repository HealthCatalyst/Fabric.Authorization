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
            queue.Enqueue(Guid.Empty);
            queue.Enqueue(roleId);

            int level = 0;

            var roleHierarchy = new HashSet<Role>();
            var visited = new HashSet<Guid>();

            while (queue.Any() && level < 10)
            {
                var topId = queue.Dequeue();
                if (topId == Guid.Empty)
                {
                    level++;
                    queue.Enqueue(Guid.Empty);
                    continue;
                }

                if (visited.Contains(topId))
                {
                    continue;
                }

                if (await this.Exists(topId).ConfigureAwait(false))
                {
                    Console.WriteLine($"Role exists, getting {topId}");
                    var role = await this.Get(topId).ConfigureAwait(false);
                    Console.WriteLine($"Got role {topId}");
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
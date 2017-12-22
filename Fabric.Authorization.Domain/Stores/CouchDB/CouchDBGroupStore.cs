using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores.Services;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public class CouchDbGroupStore : FormattableIdentifierStore<string, Group>, IGroupStore
    {
        private readonly IRoleStore _roleStore;
        private readonly IUserStore _userStore;
        private const string IdDelimiter = "-:-:";

        public CouchDbGroupStore(
            IDocumentDbService dbService,
            ILogger logger,
            IEventContextResolverService eventContextResolverService,
            IIdentifierFormatter identifierFormatter, 
            IRoleStore roleStore,
            IUserStore userStore) : base(dbService, logger, eventContextResolverService, identifierFormatter)
        {
            _roleStore = roleStore;
            _userStore = userStore;
        }

        public override async Task<Group> Add(Group group)
        {
            // this will catch older active records that do not have the unique identifier appended to the ID
            var exists = await Exists(group.Id).ConfigureAwait(false);
            if (exists)
            {
                throw new AlreadyExistsException<Group>($"Group id {group.Id} already exists. Please provide a new id.");
            }

            // append unique identifier to document ID
            group.Id = $"{group.Id}{IdDelimiter}{DateTime.UtcNow.Ticks}";
            return await Add(FormatId(group.Id), group);
        }

        public override async Task<Group> Get(string id)
        {
            try
            {
                return await base.Get(FormatId(id)).ConfigureAwait(false);
            }
            catch (NotFoundException<Group>)
            {
                Logger.Debug($"Exact match for Group {id} not found.");

                // now attempt to find a group that starts with the supplied ID
                var groups = await DocumentDbService.GetDocuments<Group>(GetGroupIdPrefix(id));
                var activeGroup = groups.FirstOrDefault(g => !g.IsDeleted);
                if (activeGroup == null)
                {
                    throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {id}");
                }

                return activeGroup;
            }
        }

        public override async Task<IEnumerable<Group>> GetAll()
        {
            return await DocumentDbService.GetDocuments<Group>("group");
        }

        public override async Task Delete(Group group)
        {
            await Delete(group.Id, group);
        }

        public override async Task Update(Group group)
        {
            group.Track(false, GetActor());
            var activeGroup = await Get(group.Id).ConfigureAwait(false);
            await ExponentialBackoff(DocumentDbService.UpdateDocument(FormatId(activeGroup.Id), group));
        }

        protected override async Task Update(string id, Group group)
        {
            group.Track(false, GetActor());
            var activeGroup = await Get(id).ConfigureAwait(false);
            await ExponentialBackoff(DocumentDbService.UpdateDocument(FormatId(activeGroup.Id), group));
        }

        public override async Task<bool> Exists(string id)
        {
            try
            {
                var result = await Get(id).ConfigureAwait(false);
                return result != null;
            }
            catch (NotFoundException<Group>)
            {
                Logger.Debug("Exists check failed for Group {id}");
                return false;
            }
        }

        private string GetGroupIdPrefix(string id)
        {
            return $"{DocumentKeyPrefix}{FormatId(id)}{IdDelimiter}";
        }

        public async Task<Group> AddRoleToGroup(Group group, Role role)
        {
            group.Roles.Add(role);
            role.Groups.Add(group.Name);

            await _roleStore.Update(role);
            await Update(group);

            return group;
        }

        public async Task<Group> DeleteRoleFromGroup(Group group, Role role)
        {        
            if (role.Groups.Any(g => string.Equals(g, group.Name, StringComparison.OrdinalIgnoreCase)))
            {
                role.Groups.Remove(group.Name);
            }

            await _roleStore.Update(role);
            await Update(group);
            return group;
        }

        public async Task<Group> AddUserToGroup(Group group, User user)
        {
            if (user.Groups.All(g => !string.Equals(g, group.Name, StringComparison.OrdinalIgnoreCase)))
            {
                user.Groups.Add(group.Name);
            }

            await _userStore.Update(user);
            await Update(group);
            return group;
        }

        public async Task<Group> DeleteUserFromGroup(Group group, User user)
        {
            if (user.Groups.Any(g => string.Equals(g, group.Name, StringComparison.OrdinalIgnoreCase)))
            {
                user.Groups.Remove(group.Name);
            }

            await _userStore.Update(user);
            await Update(group);
            return group;
        }
    }
}
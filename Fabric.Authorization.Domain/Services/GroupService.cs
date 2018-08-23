using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class GroupService
    {
        // TODO: move this out of GroupService to another library
        public static readonly Func<Role, string, string, bool> RoleFilter = (role, grain, securableItem) =>
        {
            var match = true;

            if (!string.IsNullOrWhiteSpace(grain))
            {
                match = role.Grain == grain;
            }

            if (match && !string.IsNullOrWhiteSpace(securableItem))
            {
                match = role.SecurableItem == securableItem;
            }

            return match;
        };

        private readonly IRoleStore _roleStore;
        private readonly IUserStore _userStore;
        private readonly IGroupStore _groupStore;
        private readonly RoleService _roleService;
        private readonly EDWAdminRoleSyncService _roleManager;

        public GroupService(
            IGroupStore groupStore,
            IRoleStore roleStore,
            IUserStore userStore,
            RoleService roleService,
            EDWAdminRoleSyncService roleManager)
        {
            _roleStore = roleStore;
            _userStore = userStore;
            _groupStore = groupStore ?? throw new ArgumentNullException(nameof(groupStore));          
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        public async Task<Group> AddGroup(Group group)
        {
            var persistedGroup = await _groupStore.Add(group);
            await _roleManager.RefreshDosAdminRolesAsync(persistedGroup.Users);

            return persistedGroup;
        }

        public async Task<Group> UpdateGroup(Group group)
        {
            try
            {
                await _groupStore.Update(group);
                return group;
            }
            catch (NotFoundException<Group> e)
            {
                throw new NotFoundException<Group>(e.Message);
            }
        }

        public async Task<Group> GetGroup(string groupName, string clientId)
        {
            var group = await _groupStore.Get(groupName);             
            var clientRoles = (await _roleService.GetRoles(clientId)).ToList();
            group.Roles = clientRoles.Intersect(group.Roles).ToList();
            return group;
        }

        public async Task DeleteGroup(Group group)
        {
            await _groupStore.Delete(group);
        }

        public async Task UpdateGroupList(IEnumerable<Group> groups)
        {
            var allGroups = (await _groupStore.GetAll() ?? Enumerable.Empty<Group>()).ToList();

            var groupList = groups.ToList();

            var groupNames = groupList.Select(g => g.Name);
            var storedGroupNames = allGroups.Select(g => g.Name);

            var toDelete = allGroups.Where(g => !groupNames.Contains(g.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            var toAdd = groupList.Where(g => !storedGroupNames.Contains(g.Name, StringComparer.OrdinalIgnoreCase)).ToList();

            foreach (var g in toDelete)
            {
                await _groupStore.Delete(g);
            }

            foreach (var g in toAdd)
            {
                await _groupStore.Add(g);
            }
        }

        public async Task<bool> Exists(Guid id)
        {
            return await _groupStore.Exists(id).ConfigureAwait(false);
        }

        public async Task<Group> AddRolesToGroup(IList<Role> rolesToAdd, string groupName)
        {
            var group = await _groupStore.Get(groupName);
            var grainSecurableItems = rolesToAdd.Select(r => new Tuple<string, string>(r.Grain, r.SecurableItem))
                .Distinct();
            var existingRoles = new List<Role>();
            foreach (var tuple in grainSecurableItems)
            {
                existingRoles.AddRange(await _roleStore.GetRoles(tuple.Item1, tuple.Item2));
            }

            var exceptions = new List<Exception>();
            foreach (var role in rolesToAdd)
            {
                if (existingRoles.All(r => r.Id != role.Id))
                {
                    exceptions.Add(new NotFoundException<Role>($"The role: {role} with Id: {role.Id} could not be found to add to the group."));
                }
                if (group.Roles.Any(r => r.Id == role.Id))
                {
                    exceptions.Add(
                        new AlreadyExistsException<Role>(
                            $"The role: {role} with Id: {role.Id} already exists for group {groupName}."));
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException("There was an issue adding roles to the group. Please see the inner exception(s) for details.", exceptions);
            }

            var persistedGroup = await _groupStore.AddRolesToGroup(group, rolesToAdd);
            await _roleManager.RefreshDosAdminRolesAsync(group.Users);

            return persistedGroup;
        }

        public async Task<Group> GetGroup(string groupName)
        {
            return await _groupStore.Get(groupName);
        }

        public async Task<Group> DeleteRolesFromGroup(string groupName, IEnumerable<Guid> roleIdsToDelete)
        {
            var group = await _groupStore.Get(groupName);
            return await _groupStore.DeleteRolesFromGroup(group, roleIdsToDelete);
        }

        public async Task<Group> AddUsersToGroup(string groupName, IList<User> usersToAdd)
        {
            var group = await _groupStore.Get(groupName);

            // only add users to a custom group
            if (!string.Equals(group.Source, GroupConstants.CustomSource, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException<Group>("The group to which you are attempting to add a user is not specified as a 'Custom' group. Only 'Custom' groups allow associations with users.");
            }

            var exceptions = new List<Exception>();
            foreach (var user in usersToAdd)
            {
                if (!group.Users.Any(u =>
                    string.Equals(u.SubjectId, user.SubjectId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(u.IdentityProvider, user.IdentityProvider, StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        // check if the user exists in the DB - if not, create it
                        await _userStore.Get($"{user.SubjectId}:{user.IdentityProvider}");
                    }
                    catch (NotFoundException<User>)
                    {
                        await _userStore.Add(new User(user.SubjectId, user.IdentityProvider));
                    }
                }
                else
                {
                    exceptions.Add(new AlreadyExistsException<User>($"The user {user.IdentityProvider}:{user.SubjectId} has already been added to the group {groupName}."));
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException("There was an issue adding users to the group. Please see the inner exception(s) for details.", exceptions);
            }

            var persistedGroup = await _groupStore.AddUsersToGroup(group, usersToAdd);
            await this._roleManager.RefreshDosAdminRolesAsync(persistedGroup.Users);

            return persistedGroup;
        }

        public async Task<Group> DeleteUserFromGroup(string groupName, string subjectId, string identityProvider)
        {
            var group = await _groupStore.Get(groupName);
            var user = await _userStore.Get($"{subjectId}:{identityProvider}");

            var groupUser = group.Users.FirstOrDefault(u => u.Id == user.Id);
            if (groupUser != null)
            {
                group.Users.Remove(groupUser);
            }

            var persistedGroup = await _groupStore.DeleteUserFromGroup(group, user);
            await this._roleManager.RefreshDosAdminRolesAsync(groupUser);

            return persistedGroup;
        }

        public async Task<Group> AddChildGroups(Group group, IEnumerable<Group> childGroups)
        {
            // do not allow non-custom group to be parent
            if (!string.Equals(group.Source, GroupConstants.CustomSource, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException<Group>($"{group.Name} is not a custom group. Only custom groups can serve as parent groups.");
            }

            // first make sure all the child groups are created
            var childGroupList = childGroups.ToList();

            try
            {
                await _groupStore.Get(childGroupList.Select(g => g.Name));
            }
            // filter out groups that already exist so we don't attempt to create an existent group
            catch (NotFoundException<Group> ex)
            {
                var missingChildGroups = childGroupList.Where(g => ex.ExceptionDetails.Select(e => e.Identifier).Contains(g.Name)).ToList();

                var invalidMissingGroups =
                    missingChildGroups.Where(g => string.IsNullOrWhiteSpace(g.Name)
                                                  || string.IsNullOrWhiteSpace(g.Source)
                                                  || string.Equals(g.Source, GroupConstants.CustomSource,
                                                      StringComparison.OrdinalIgnoreCase)).ToList();

                if (invalidMissingGroups.Any())
                {
                    throw new BadRequestException<Group>(
                        $"The following child groups do not exist in our database and cannot be created due to 1 or more of the following reasons: 1) missing GroupName, 2) missing GroupSource or 3) the GroupSource is incorrectly specified as Custom: {string.Join(", ", invalidMissingGroups.Select(g => g.Name))}");
                }
                await _groupStore.Add(missingChildGroups);
            }

            var childGroupNameList = childGroupList.Select(g => g.Name).ToList();

            // do not allow custom groups to be children
            childGroups = (await _groupStore.Get(childGroupNameList)).ToList();
            var customGroups = childGroups.Where(g => g.Source == GroupConstants.CustomSource).ToList();
            if (customGroups.Any())
            {
                throw new BadRequestException<Group>($"Only directory groups can be child groups. The following groups are Custom groups: {string.Join(", ", customGroups)} ");
            }

            // if association already exists, return 409
            var existingAssociations = group.Children.Where(c => childGroupNameList.Contains(c.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            if (existingAssociations.Any())
            {
                throw new AlreadyExistsException<Group>($"The following groups are already children of group {group.Name}: {string.Join(", ", existingAssociations.Select(g => g.Name))}");
            }

            var persistedGroup = await _groupStore.AddChildGroups(group, childGroups);
            await this._roleManager.RefreshDosAdminRolesAsync(group.Children.SelectMany(childGroup => childGroup.Users));

            return persistedGroup;
        }

        public async Task<Group> RemoveChildGroups(Group group, IEnumerable<string> childGroupNames)
        {
            var childGroups = await _groupStore.Get(childGroupNames);
            var childUsers = childGroups.SelectMany(g => g.Users);

            var persistedGroup = await _groupStore.RemoveChildGroups(group, childGroups);
            await this._roleManager.RefreshDosAdminRolesAsync(childUsers);

            return persistedGroup;
        }

        public async Task<IEnumerable<Group>> GetGroups(string name, string type)
        {
            if (!string.IsNullOrEmpty(type))
            {
                if (!type.Equals(GroupConstants.CustomSource, StringComparison.OrdinalIgnoreCase) && 
                    !type.Equals(GroupConstants.DirectorySource, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Invalid type provided. If provided valid values are custom or directory");
                }
            }

            var groups = await _groupStore.GetGroups(name, type);
            return groups;
        }
    }
}
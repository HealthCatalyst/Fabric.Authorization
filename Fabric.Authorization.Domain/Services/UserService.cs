using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class UserService
    {
        private readonly IUserStore _userStore;
        private readonly IRoleStore _roleStore;

        public UserService(IUserStore userStore, IRoleStore roleStore)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
        }

        public async Task<User> GetUser(string subjectId, string identityProvider)
        {
            return await _userStore.Get($"{subjectId}:{identityProvider}");
        }

        public async Task<IEnumerable<Group>> GetGroupsForUser(string subjectId, string identityProvider, bool flattenChildGroups = false)
        {
            var user = await _userStore.Get($"{subjectId}:{identityProvider}");
            if (user == null)
            {
                return new List<Group>();
            }
            
            if (!flattenChildGroups)
            {
                return user.Groups;
            }

            var childGroups = user.Groups?.SelectMany(g => g.Children);
            return user.Groups?.Union(childGroups ?? new List<Group>());
        }

        public async Task<ICollection<Role>> GetRolesForUser(string subjectId, string identityProvider)
        {
            var user = await _userStore.Get($"{subjectId}:{identityProvider}");
            return user != null ? user.Roles : new List<Role>();
        }

        public async Task<User> AddUser(User user)
        {
            return await _userStore.Add(user);
        }

        public async Task<bool> Exists(string subjectId, string identityProvider)
        {
            return await _userStore.Exists($"{subjectId}:{identityProvider}");
        }

        public async Task<User> AddRolesToUser(IList<Role> rolesToAdd, string subjectId, string identityProvider)
        {
            var user = await _userStore.Get($"{subjectId}:{identityProvider}");
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
                    exceptions.Add(new NotFoundException<Role>($"The role: {role} with Id: {role.Id} could not be found to add to the user."));
                }
                if (user.Roles.Any(r => r.Id == role.Id))
                {
                    exceptions.Add(
                        new AlreadyExistsException<Role>(
                            $"The role: {role} with Id: {role.Id} already exists for the user."));
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException("There was an issue adding roles to the user. Please see the inner exception(s) for details.", exceptions);
            }

            return await _userStore.AddRolesToUser(user, rolesToAdd);
        }

        public async Task<User> DeleteRolesFromUser(IList<Role> rolesToDelete, string subjectId,
            string identityProvider)
        {
            var user = await _userStore.Get($"{subjectId}:{identityProvider}");
            var exceptions = new List<Exception>();
            foreach (var role in rolesToDelete)
            {
                if (user.Roles.All(r => r.Id != role.Id))
                {
                    exceptions.Add(
                        new NotFoundException<Role>(
                            $"The role: {role} with Id: {role.Id} does not exist for the user: {subjectId} and could not be deleted."));
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(
                    "There was an issue deleting roles for the user. Please see the inner exception(s) for the details.",
                    exceptions);
            }

            return await _userStore.DeleteRolesFromUser(user, rolesToDelete);
        }
    }
}
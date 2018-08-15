using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IGroupStore : IGenericStore<Guid, Group>
    {
        Task<Group> Get(string name);
        Task<IEnumerable<Group>> Get(IEnumerable<string> groupNames);
        Task<bool> Exists(string name);
        Task<Group> AddRolesToGroup(Group group, IEnumerable<Role> rolesToAdd);
        Task<Group> DeleteRolesFromGroup(Group group, IEnumerable<Guid> roleIdsToDelete);
        Task<Group> AddUserToGroup(Group group, User user);
        Task<Group> AddUsersToGroup(Group group, IEnumerable<User> usersToAdd);
        Task<Group> DeleteUserFromGroup(Group group, User user);
        Task<Group> AddChildGroups(Group group, IEnumerable<Group> childGroups);
        Task<Group> RemoveChildGroups(Group group, IEnumerable<Group> childGroups);
        Task<IEnumerable<Group>> GetGroups(string name, string type);
    }
}
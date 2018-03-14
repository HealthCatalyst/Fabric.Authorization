using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IGroupStore : IGenericStore<string, Group>
    {
        Task<Group> AddRoleToGroup(Group group, Role role);
        Task<Group> AddRolesToGroup(Group group, IEnumerable<Role> rolesToAdd);
        Task<Group> DeleteRoleFromGroup(Group group, Role role);
        Task<Group> AddUserToGroup(Group group, User user);
        Task<Group> AddUsersToGroup(Group group, IEnumerable<User> usersToAdd);
        Task<Group> DeleteUserFromGroup(Group group, User user);
    }
}
using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IGroupStore : IGenericStore<string, Group>
    {
        Task<Group> AddRoleToGroup(string groupName, Guid roleId);
        Task<Group> DeleteRoleFromGroup(string groupName, Guid roleId);
        Task<Group> AddUserToGroup(string groupName, string subjectId, string identityProvider);
        Task<Group> DeleteUserFromGroup(string groupName, string subjectId, string identityProvider);
    }
}
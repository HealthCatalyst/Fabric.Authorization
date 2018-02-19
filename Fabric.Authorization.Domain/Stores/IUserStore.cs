using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IUserStore : IGenericStore<string, User>
    {
        Task<User> AddRolesToUser(User user, IList<Role> roles);
        Task<User> DeleteRolesFromUser(User user, IList<Role> roles);
    }
}
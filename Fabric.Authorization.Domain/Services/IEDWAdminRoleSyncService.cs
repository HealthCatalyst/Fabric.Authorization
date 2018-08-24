using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Services
{
    public interface IEDWAdminRoleSyncService
    {
        Task RefreshDosAdminRolesAsync(IEnumerable<User> users);
        Task RefreshDosAdminRolesAsync(User user);
    }
}
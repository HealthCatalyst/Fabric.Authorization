using System.Threading.Tasks;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Microsoft.EntityFrameworkCore;
using Client = Fabric.Authorization.Persistence.SqlServer.EntityModels.Client;
using Group = Fabric.Authorization.Persistence.SqlServer.EntityModels.Group;
using Permission = Fabric.Authorization.Persistence.SqlServer.EntityModels.Permission;
using Role = Fabric.Authorization.Persistence.SqlServer.EntityModels.Role;
using SecurableItem = Fabric.Authorization.Persistence.SqlServer.EntityModels.SecurableItem;
using User = Fabric.Authorization.Persistence.SqlServer.EntityModels.User;

namespace Fabric.Authorization.Persistence.SqlServer.Services
{
    public interface IAuthorizationDbContext
    {
        DbSet<Client> Clients { get; set; }
        DbSet<SecurableItem> SecurableItems { get; set; }
        DbSet<Group> Groups { get; set; }
        DbSet<Role> Roles { get; set; }
        DbSet<GroupRole> GroupRoles { get; set; }
        DbSet<Permission> Permissions { get; set; }
        DbSet<RolePermission> RolePermissions { get; set; }
        DbSet<User> Users { get; set; }
        Task<int> SaveChangesAsync();
        int SaveChanges();
    }
}
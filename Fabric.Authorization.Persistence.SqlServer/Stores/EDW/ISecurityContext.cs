using Fabric.Authorization.Persistence.SqlServer.EntityModels.EDW;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Stores.EDW
{
    public interface ISecurityContext
    {
        DbSet<EDWIdentity> EDWIdentities { get; set; }

        DbSet<EDWRole> EDWRoles { get; set; }

        DbSet<EDWIdentityRole> EDWIdentityRoles { get; set; }

        int SaveChanges();
    }
}

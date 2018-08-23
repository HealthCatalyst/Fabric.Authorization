using System;
using Fabric.Authorization.Domain.Models.EDW;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Stores.EDW
{
    public interface ISecurityContext : IDisposable
    {
        DbSet<EDWIdentity> EDWIdentities { get; set; }

        DbSet<EDWRole> EDWRoles { get; set; }

        DbSet<EDWIdentityRole> EDWIdentityRoles { get; set; }

        int SaveChanges();
    }
}

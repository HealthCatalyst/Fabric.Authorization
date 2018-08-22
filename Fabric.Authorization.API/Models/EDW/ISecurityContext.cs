namespace Catalyst.Security.Services
{
    using System;
    using Fabric.Authorization.API.Models.EDW;
    using Microsoft.EntityFrameworkCore;

    public interface ISecurityContext : IDisposable
    {
        DbSet<EDWIdentity> EDWIdentities { get; set; }

        DbSet<EDWRole> EDWRoles { get; set; }

        DbSet<EDWIdentityRole> EDWIdentityRoles { get; set; }

        int SaveChanges();

        ISecurityContext CreateContext();
    }
}

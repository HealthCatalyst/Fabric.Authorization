using Catalyst.Security.Services;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.API.Models.EDW
{
    public class SecurityContext : DbContext, ISecurityContext
    {
        protected readonly ConnectionStrings connectionStrings;

        public DbSet<EDWIdentity> EDWIdentities { get; set; }
        public DbSet<EDWRole> EDWRoles { get; set; }
        public DbSet<EDWIdentityRole> EDWIdentityRoles { get; set; }

        public SecurityContext() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(connectionStrings.EDWAdminDatabase);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                return;
            }

            modelBuilder.Entity<EDWIdentityRole>()
                .HasKey(identityRole => new { identityRole.IdentityId, identityRole.RoleId });
            modelBuilder.Entity<EDWIdentityRole>()
                .HasOne(identityRole => identityRole.EDWRole)
                .WithMany(role => role.EDWIdentityRoles)
                .HasForeignKey(identityRole => identityRole.RoleId);
            modelBuilder.Entity<EDWIdentityRole>()
                .HasOne(identityRole => identityRole.EDWIdentity)
                .WithMany(role => role.EDWIdentityRoles)
                .HasForeignKey(identityRole => identityRole.RoleId);
        }

        public ISecurityContext CreateContext()
        {
            return new SecurityContext();
        }
    }
}

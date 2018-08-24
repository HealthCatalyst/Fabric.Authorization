using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Fabric.Authorization.Persistence.SqlServer.EntityModels.EDW;
using Fabric.Authorization.Persistence.SqlServer.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Stores.EDW
{
    public class SecurityContext : DbContext, ISecurityContext
    {
        protected readonly ConnectionStrings ConnectionStrings;

        public DbSet<EDWIdentity> EDWIdentities { get; set; }
        public DbSet<EDWRole> EDWRoles { get; set; }
        public DbSet<EDWIdentityRole> EDWIdentityRoles { get; set; }

        public SecurityContext(ConnectionStrings connectionStrings)
        {
            ConnectionStrings = connectionStrings;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionStrings.EDWAdminDatabase);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("CatalystAdmin");
            modelBuilder.ConfigureEDWIdentityRole();
            modelBuilder.ConfigureEDWIdentity();
            modelBuilder.ConfigureEDWRole();

            base.OnModelCreating(modelBuilder);
        }
    }
}

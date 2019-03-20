using Fabric.Authorization.Persistence.SqlServer.EntityModels.EDW;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Extensions
{
    public static class EDWModelBuilderExtensions
    {
        public static void ConfigureEDWIdentityRole(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EDWIdentityRole>(entity =>
            {
                entity.ToTable("IdentityRoleBASE");

                entity.HasKey(p => p.IdentityRoleID);

                entity.Property(p => p.IdentityRoleID)
                    .ValueGeneratedOnAdd()
                    .UseSqlServerIdentityColumn();

                entity.Property(p => p.IdentityID)
                    .IsRequired();

                entity.Property(p => p.RoleID)
                    .IsRequired();

                entity.HasOne(p => p.EDWIdentity)
                    .WithMany(p => p.EDWIdentityRoles)
                    .HasForeignKey(k => k.IdentityID);

                entity.HasOne(p => p.EDWRole)
                    .WithMany(role => role.EDWIdentityRoles)
                    .HasForeignKey(k => k.RoleID);
            });
        }

        public static void ConfigureEDWIdentity(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EDWIdentity>(entity =>
            {
                entity.ToTable("IdentityBASE");

                entity.HasKey(p => p.Id);
                
                entity.Property(p => p.Id)
                    .HasColumnName("IdentityID")
                    .ValueGeneratedOnAdd()
                    .UseSqlServerIdentityColumn();

                entity.Property(p => p.Name)
                    .HasColumnName("IdentityNM")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.HasMany(p => p.EDWIdentityRoles)
                    .WithOne(p => p.EDWIdentity)
                    .HasForeignKey(p => p.IdentityID);
            });
        }

        public static void ConfigureEDWRole(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EDWRole>(entity =>
            {
                entity.ToTable("RoleBASE");                

                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id)
                    .HasColumnName("RoleID")
                    .ValueGeneratedOnAdd()
                    .UseSqlServerIdentityColumn();

                entity.Property(p => p.Name)
                    .HasColumnName("RoleNM")
                    .HasMaxLength(255)
                    .IsRequired();
                
                entity.Property(p => p.Description)
                    .HasColumnName("RoleDSC")
                    .HasMaxLength(4000);

                entity.HasMany(p => p.EDWIdentityRoles)
                    .WithOne(p => p.EDWRole)
                    .HasForeignKey(p => p.RoleID);
            });
        }
    }
}

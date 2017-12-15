using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static void ConfigureClient(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clients");

                entity.HasIndex(i => i.SecurableItemId)
                    .HasName("IX_Clients_SecurableItemId");

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.SecurableItemId)
                    .IsRequired();

                entity.Property(p => p.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.CreatedDateTimeUtc)
                    .HasColumnType("datetime")
                    .IsRequired();

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.HasOne(e => e.TopLevelSecurableItem)
                    .WithMany(d => d.Clients)
                    .HasForeignKey(d => d.SecurableItemId);
            });
        }

        public static void ConfigureSecurableItem(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SecurableItem>(entity =>
            {
                entity.ToTable("SecurableItems");

                entity.HasIndex(i => i.SecurableItemId)
                    .HasName("IX_SecurableItems_SecurableItemId");

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.SecurableItemId)
                    .IsRequired();

                entity.Property(p => p.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.CreatedDateTimeUtc)
                    .HasColumnType("datetime")
                    .IsRequired();

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");
            });
        }

        public static void ConfigurePermission(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("Permissions");

                entity.HasIndex(e => e.SecurableItemId)
                    .HasName("IX_Permissions_SecurableItemId");

                entity.Property(e => e.SecurableItemId)
                    .IsRequired();

                entity.Property(e => e.Grain)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedDateTimeUtc)
                    .HasColumnType("datetime")
                    .IsRequired();

                entity.Property(e => e.ModifiedDateTimeUtc)
                    .HasColumnType("datetime");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.HasOne(e => e.SecurableItem)
                    .WithMany(e => e.Permissions)
                    .HasForeignKey(e => e.SecurableItemId);

                entity.HasMany(e => e.RolePermissions)
                    .WithOne(e => e.Permission)
                    .HasForeignKey(e => e.PermissionId);

                entity.HasMany(e => e.UserPermissions)
                    .WithOne(e => e.Permission)
                    .HasForeignKey(e => e.UserId);
            });
        }

        public static void ConfigureRole(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");

                entity.HasIndex(e => e.SecurableItemId)
                    .HasName("IX_Roles_SecurableItemId");

                entity.Property(e => e.SecurableItemId)
                    .IsRequired();

                entity.Property(e => e.Grain)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedDateTimeUtc)
                    .HasColumnType("datetime")
                    .IsRequired();

                entity.Property(e => e.ModifiedDateTimeUtc)
                    .HasColumnType("datetime");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.HasOne(e => e.SecurableItem)
                    .WithMany(e => e.Roles)
                    .HasForeignKey(e => e.SecurableItemId);

                entity.HasMany(e => e.GroupRoles)
                    .WithOne(e => e.Role)
                    .HasForeignKey(e => e.RoleId);

                entity.HasMany(e => e.RolePermissions)
                    .WithOne(e => e.Role)
                    .HasForeignKey(e => e.RoleId);
            });
        }

        public static void ConfigureGroup(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Group>(entity =>
            {
                entity.ToTable("Groups");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Source)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedDateTimeUtc)
                    .HasColumnType("datetime")
                    .IsRequired();

                entity.Property(e => e.ModifiedDateTimeUtc)
                    .HasColumnType("datetime");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.HasMany(e => e.GroupRoles)
                    .WithOne(e => e.Group)
                    .HasForeignKey(e => e.GroupId);

                entity.HasMany(e => e.UserGroups)
                    .WithOne(e => e.Group)
                    .HasForeignKey(e => e.GroupId);
            });
        }

        public static void ConfigureUser(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.Property(e => e.SubjectId)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.IdentityProvider)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedDateTimeUtc)
                    .HasColumnType("datetime")
                    .IsRequired();

                entity.Property(e => e.ModifiedDateTimeUtc)
                    .HasColumnType("datetime");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.HasMany(e => e.UserGroups)
                    .WithOne(e => e.User)
                    .HasForeignKey(e => e.UserId);

                entity.HasMany(e => e.UserPermissions)
                    .WithOne(e => e.User)
                    .HasForeignKey(e => e.UserId);
            });
        }

        public static void ConfigureGroupRole(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GroupRole>(entity =>
            {
                entity.ToTable("GroupRoles");

                entity.HasOne(e => e.Group)
                    .WithMany(e => e.GroupRoles)
                    .HasForeignKey(e => e.GroupId);

                entity.HasOne(e => e.Role)
                    .WithMany(e => e.GroupRoles)
                    .HasForeignKey(e => e.RoleId);
            });
        }

        public static void ConfigureRolePermission(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("RolePermissions");

                entity.HasOne(e => e.Role)
                    .WithMany(e => e.RolePermissions)
                    .HasForeignKey(e => e.RoleId);

                entity.HasOne(e => e.Permission)
                    .WithMany(e => e.RolePermissions)
                    .HasForeignKey(e => e.PermissionId);

                entity.Property(e => e.PermissionAction)
                    .IsRequired()
                    .HasDefaultValueSql("0");
            });
        }

        public static void ConfigureUserGroup(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserGroup>(entity =>
            {
                entity.ToTable("UserGroups");

                entity.HasOne(e => e.User)
                    .WithMany(e => e.UserGroups)
                    .HasForeignKey(e => e.UserId);

                entity.HasOne(e => e.Group)
                    .WithMany(e => e.UserGroups)
                    .HasForeignKey(e => e.GroupId);
            });
        }

        public static void ConfigureUserPermission(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.ToTable("UserPermissions");

                entity.HasOne(e => e.User)
                    .WithMany(e => e.UserPermissions)
                    .HasForeignKey(e => e.UserId);

                entity.HasOne(e => e.Permission)
                    .WithMany(e => e.UserPermissions)
                    .HasForeignKey(e => e.PermissionId);

                entity.Property(e => e.PermissionAction)
                    .IsRequired()
                    .HasDefaultValueSql("0");
            });
        }
    }
}
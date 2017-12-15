using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;

namespace Fabric.Authorization.Persistence.SqlServer.Migrations
{
    [DbContext(typeof(AuthorizationDbContext))]
    [Migration("20171215210021_Test")]
    partial class Test
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.5")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.Client", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClientId");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.Property<DateTime>("CreatedDateTimeUtc")
                        .HasColumnType("datetime");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("0");

                    b.Property<string>("ModifiedBy");

                    b.Property<DateTime?>("ModifiedDateTimeUtc");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.Property<int>("SecurableItemId");

                    b.HasKey("Id");

                    b.HasIndex("SecurableItemId")
                        .HasName("IX_Clients_SecurableItemId");

                    b.ToTable("Clients");
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.Group", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.Property<DateTime>("CreatedDateTimeUtc")
                        .HasColumnType("datetime");

                    b.Property<string>("GroupId");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("0");

                    b.Property<string>("ModifiedBy");

                    b.Property<DateTime?>("ModifiedDateTimeUtc")
                        .HasColumnType("datetime");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.HasKey("Id");

                    b.ToTable("Groups");
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.GroupRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CreatedBy");

                    b.Property<DateTime>("CreatedDateTimeUtc");

                    b.Property<int>("GroupId");

                    b.Property<bool>("IsDeleted");

                    b.Property<string>("ModifiedBy");

                    b.Property<DateTime?>("ModifiedDateTimeUtc");

                    b.Property<int>("RoleId");

                    b.HasKey("Id");

                    b.HasAlternateKey("GroupId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("GroupRoles");
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.GroupUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CreatedBy");

                    b.Property<DateTime>("CreatedDateTimeUtc");

                    b.Property<int>("GroupId");

                    b.Property<bool>("IsDeleted");

                    b.Property<string>("ModifiedBy");

                    b.Property<DateTime?>("ModifiedDateTimeUtc");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasAlternateKey("UserId", "GroupId");

                    b.HasIndex("GroupId");

                    b.ToTable("GroupUsers");
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.Permission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.Property<DateTime>("CreatedDateTimeUtc")
                        .HasColumnType("datetime");

                    b.Property<string>("Grain")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("0");

                    b.Property<string>("ModifiedBy");

                    b.Property<DateTime?>("ModifiedDateTimeUtc")
                        .HasColumnType("datetime");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.Property<Guid>("PermissionId");

                    b.Property<int>("SecurableItemId");

                    b.HasKey("Id");

                    b.HasIndex("SecurableItemId")
                        .HasName("IX_Permissions_SecurableItemId");

                    b.ToTable("Permissions");
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.Role", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.Property<DateTime>("CreatedDateTimeUtc")
                        .HasColumnType("datetime");

                    b.Property<string>("Grain")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("0");

                    b.Property<string>("ModifiedBy");

                    b.Property<DateTime?>("ModifiedDateTimeUtc")
                        .HasColumnType("datetime");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.Property<int?>("ParentRoleId");

                    b.Property<Guid>("RoleId");

                    b.Property<int>("SecurableItemId");

                    b.HasKey("Id");

                    b.HasIndex("ParentRoleId");

                    b.HasIndex("SecurableItemId")
                        .HasName("IX_Roles_SecurableItemId");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.RolePermission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CreatedBy");

                    b.Property<DateTime>("CreatedDateTimeUtc");

                    b.Property<bool>("IsDeleted");

                    b.Property<string>("ModifiedBy");

                    b.Property<DateTime?>("ModifiedDateTimeUtc");

                    b.Property<int>("PermissionAction")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("0");

                    b.Property<int>("PermissionId");

                    b.Property<int>("RoleId");

                    b.HasKey("Id");

                    b.HasAlternateKey("RoleId", "PermissionId");

                    b.HasIndex("PermissionId");

                    b.ToTable("RolePermissions");
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.SecurableItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.Property<DateTime>("CreatedDateTimeUtc")
                        .HasColumnType("datetime");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("0");

                    b.Property<string>("ModifiedBy");

                    b.Property<DateTime?>("ModifiedDateTimeUtc");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.Property<Guid>("SecurableItemId");

                    b.Property<int?>("SecurableItemId1");

                    b.HasKey("Id");

                    b.HasIndex("SecurableItemId")
                        .HasName("IX_SecurableItems_SecurableItemId");

                    b.HasIndex("SecurableItemId1");

                    b.ToTable("SecurableItems");
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.Property<DateTime>("CreatedDateTimeUtc")
                        .HasColumnType("datetime");

                    b.Property<string>("IdentityProvider")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("0");

                    b.Property<string>("ModifiedBy");

                    b.Property<DateTime?>("ModifiedDateTimeUtc")
                        .HasColumnType("datetime");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.Property<string>("SubjectId")
                        .IsRequired()
                        .HasMaxLength(200);

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.UserPermission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CreatedBy");

                    b.Property<DateTime>("CreatedDateTimeUtc");

                    b.Property<bool>("IsDeleted");

                    b.Property<string>("ModifiedBy");

                    b.Property<DateTime?>("ModifiedDateTimeUtc");

                    b.Property<int>("PermissionAction")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("0");

                    b.Property<int>("PermissionId");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasAlternateKey("UserId", "PermissionId");

                    b.HasIndex("PermissionId");

                    b.ToTable("UserPermissions");
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.Client", b =>
                {
                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.SecurableItem", "TopLevelSecurableItem")
                        .WithMany("Clients")
                        .HasForeignKey("SecurableItemId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.GroupRole", b =>
                {
                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.Group", "Group")
                        .WithMany("GroupRoles")
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.Role", "Role")
                        .WithMany("GroupRoles")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.GroupUser", b =>
                {
                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.Group", "Group")
                        .WithMany("GroupUsers")
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.User", "User")
                        .WithMany("GroupUsers")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.Permission", b =>
                {
                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.SecurableItem", "SecurableItem")
                        .WithMany("Permissions")
                        .HasForeignKey("SecurableItemId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.Role", b =>
                {
                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.Role", "ParentRole")
                        .WithMany("ChildRoles")
                        .HasForeignKey("ParentRoleId");

                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.SecurableItem", "SecurableItem")
                        .WithMany("Roles")
                        .HasForeignKey("SecurableItemId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.RolePermission", b =>
                {
                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.Permission", "Permission")
                        .WithMany("RolePermissions")
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.Role", "Role")
                        .WithMany("RolePermissions")
                        .HasForeignKey("RoleId");
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.SecurableItem", b =>
                {
                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.SecurableItem")
                        .WithMany("SecurableItems")
                        .HasForeignKey("SecurableItemId1");
                });

            modelBuilder.Entity("Fabric.Authorization.Persistence.SqlServer.EntityModels.UserPermission", b =>
                {
                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.Permission", "Permission")
                        .WithMany("UserPermissions")
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Fabric.Authorization.Persistence.SqlServer.EntityModels.User", "User")
                        .WithMany("UserPermissions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
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
    public class AuthorizationDbContext : DbContext, IAuthorizationDbContext
    {
        private readonly IEventContextResolverService _eventContextResolverService;

        public AuthorizationDbContext(DbContextOptions options, IEventContextResolverService eventContextResolverService) 
            : base(options)
        {
            _eventContextResolverService = eventContextResolverService;
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<SecurableItem> SecurableItems { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<GroupRole> GroupRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }


        public async Task<int> SaveChangesAsync()
        {
            OnSaveChanges();

            return await base.SaveChangesAsync();
        }

        public override int SaveChanges()
        {
            OnSaveChanges();

            return base.SaveChanges();
        }

        private void OnSaveChanges()
        {
            var entities = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entityEntry in entities)
            {
                var trackableEntity = entityEntry.Entity as ITrackable;
                if (trackableEntity == null)
                {
                    continue;
                }

                if (entityEntry.State == EntityState.Added)
                {
                    trackableEntity.CreatedDateTimeUtc = DateTime.UtcNow;
                    trackableEntity.CreatedBy = GetActor();
                }
                else if (entityEntry.State == EntityState.Modified)
                {
                    trackableEntity.ModifiedDateTimeUtc = DateTime.UtcNow;
                    trackableEntity.ModifiedBy = GetActor();
                }
            }
        }

        private string GetActor()
        {
            return (_eventContextResolverService.Subject ?? _eventContextResolverService.ClientId) ?? "anonymous";
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clients");

                entity.HasIndex(i => i.SecurableItemId)
                    .HasName("IX_Client_SecurableItemId");

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

            modelBuilder.Entity<SecurableItem>(entity =>
            {
                entity.ToTable("SecurableItems");

                entity.HasIndex(i => i.SecurableItemId)
                    .HasName("IX_SecurableItem_SecurableItemId");

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
    }
}
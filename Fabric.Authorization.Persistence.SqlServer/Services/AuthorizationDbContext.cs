using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Client = Fabric.Authorization.Persistence.SqlServer.EntityModels.Client;
using Grain = Fabric.Authorization.Persistence.SqlServer.EntityModels.Grain;
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
        protected readonly ConnectionStrings ConnectionStrings;
        
        public AuthorizationDbContext(IEventContextResolverService eventContextResolverService, ConnectionStrings connectionStrings)
        {
            _eventContextResolverService = eventContextResolverService;
            ConnectionStrings = connectionStrings;
        }
        public DbSet<Grain> Grains { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<SecurableItem> SecurableItems { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<GroupRole> GroupRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<GroupUser> GroupUsers { get; set; }
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
            modelBuilder.ConfigureGrain();
            modelBuilder.ConfigureClient();
            modelBuilder.ConfigureSecurableItem();
            modelBuilder.ConfigurePermission();
            modelBuilder.ConfigureRole();
            modelBuilder.ConfigureGroup();
            modelBuilder.ConfigureUser();
            modelBuilder.ConfigureGroupRole();
            modelBuilder.ConfigureGroupUser();
            modelBuilder.ConfigureRolePermission();
            modelBuilder.ConfigureUserPermission();

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionStrings.AuthorizationDatabase);
        }

        public override EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        {
            return base.Entry(entity);
        }
    }
}
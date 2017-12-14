using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Client = Fabric.Authorization.Persistence.SqlServer.EntityModels.Client;
using SecurableItem = Fabric.Authorization.Persistence.SqlServer.EntityModels.SecurableItem;

namespace Fabric.Authorization.Persistence.SqlServer.Services
{
    public class AuthorizationDbContext : DbContext, IAuthorizationDbContext
    {
        public AuthorizationDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<SecurableItem> SecurableItems { get; set; }

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
            var entities = base.ChangeTracker.Entries()
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
                    trackableEntity.CreatedBy =
                        "placeholder"; //(_userResolverService.Subject ?? _userResolverService.ClientId) ?? "anonymous";
                }
                else if (entityEntry.State == EntityState.Modified)
                {
                    trackableEntity.ModifiedDateTimeUtc = DateTime.UtcNow;
                    trackableEntity.ModifiedBy =
                        "placeholder"; //(_userResolverService.Subject ?? _userResolverService.ClientId) ?? "anonymous";
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
    }
}
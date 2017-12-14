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
                    trackableEntity.CreatedBy = "placeholder"; 
                }
                else if (entityEntry.State == EntityState.Modified)
                {
                    trackableEntity.ModifiedDateTimeUtc = DateTime.UtcNow;
                    trackableEntity.ModifiedBy = "placeholder"; 
                }
            }
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
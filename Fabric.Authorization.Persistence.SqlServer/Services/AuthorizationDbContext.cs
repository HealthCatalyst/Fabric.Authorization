using System.Threading.Tasks;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Services
{
    public class AuthorizationDbContext : DbContext, IAuthorizationDbContext
    {
        public AuthorizationDbContext(DbContextOptions options) : base(options)
        {
            
        }

        public DbSet<Client> Clients { get; set; }
        public async Task<int> SaveChangesAsync()
        {
            return await base.SaveChangesAsync();
        }
    }
}
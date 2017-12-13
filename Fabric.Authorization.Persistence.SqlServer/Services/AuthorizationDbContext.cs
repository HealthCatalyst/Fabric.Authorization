using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Services
{
    public class AuthorizationDbContext : DbContext, IAuthorizationDbContext
    {
        public AuthorizationDbContext(DbContextOptions options) : base(options)
        {
            
        }
    }
}
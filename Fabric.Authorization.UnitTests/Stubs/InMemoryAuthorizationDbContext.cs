using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.UnitTests.Stubs
{
    public class InMemoryAuthorizationDbContext : AuthorizationDbContext
    {
        public InMemoryAuthorizationDbContext(IEventContextResolverService eventContextResolverService, ConnectionStrings connectionStrings) : base(eventContextResolverService, connectionStrings)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase();
        }
    }
}

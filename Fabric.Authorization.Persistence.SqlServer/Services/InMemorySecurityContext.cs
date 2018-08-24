using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Fabric.Authorization.Persistence.SqlServer.Stores.EDW;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Services
{
    public class InMemorySecurityContext : SecurityContext
    {
        public InMemorySecurityContext(ConnectionStrings connectionStrings)
            : base(connectionStrings)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(databaseName: ConnectionStrings.EDWAdminDatabase);
        }
    }
}

using System.Linq;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Fabric.Authorization.UnitTests.Stubs;
using Xunit;

namespace Fabric.Authorization.UnitTests.DbBootstrappers
{
    public class SqlServerDbBootstrapperTests
    {
        [Fact]
        public void Setup_CreatesBuiltInGrains_Success()
        {
            var dbContext = CreateDbContext();
            Bootstrap(dbContext, new Domain.Defaults.Authorization());
            var grains = dbContext.Grains.ToList();
            Assert.Equal(2, grains.Count);
        }

        [Fact]
        public void Setup_UpdatesBuiltInGrains_Success()
        {
            var dbContext = CreateDbContext();
            var authorizationDefaults = new Domain.Defaults.Authorization();
            Bootstrap(dbContext, authorizationDefaults);
            var grains = dbContext.Grains.ToList();
            Assert.Equal(2, grains.Count);

            var dosGrain = authorizationDefaults.Grains.First(g => g.Name == "dos");
            dosGrain.RequiredWriteScopes.Add("fabric/authorization.write");

            Bootstrap(dbContext, authorizationDefaults);

            var updatedDosGrain = dbContext.Grains.First(g => g.Name == "dos");
            Assert.Equal("fabric/authorization.dos.write;fabric/authorization.write", updatedDosGrain.RequiredWriteScopes);
        }

        private void Bootstrap(IAuthorizationDbContext dbContext, Domain.Defaults.Authorization authorizationDefaults)
        {
            var sqlServerBootStrapper = new SqlServerDbBootstrapper(dbContext, authorizationDefaults);
            sqlServerBootStrapper.Setup();
        }

        private InMemoryAuthorizationDbContext CreateDbContext()
        {
            return new InMemoryAuthorizationDbContext(new NoOpEventContextResolverService(), new ConnectionStrings { AuthorizationDatabase = "somestring" });
        }
    }
}

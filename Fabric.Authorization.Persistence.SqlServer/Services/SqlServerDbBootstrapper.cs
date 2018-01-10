using AutoMapper;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.SqlServer.Mappers;

namespace Fabric.Authorization.Persistence.SqlServer.Services
{
    public class SqlServerDbBootstrapper : IDbBootstrapper
    {
        static SqlServerDbBootstrapper()
        {
            //register all the automapper profiles - only ever want to call this once
            //http://automapper.readthedocs.io/en/latest/Configuration.html#assembly-scanning-for-auto-configuration
            Mapper.Initialize(cfg => cfg.AddProfiles(typeof(SqlServerDbBootstrapper)));
           // Mapper.AssertConfigurationIsValid();            
        }

        public void Setup()
        {
            
        }
    }
}
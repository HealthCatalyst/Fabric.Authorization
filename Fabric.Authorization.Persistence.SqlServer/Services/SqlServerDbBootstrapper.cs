using AutoMapper;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.SqlServer.Mappers;

namespace Fabric.Authorization.Persistence.SqlServer.Services
{
    public class SqlServerDbBootstrapper : IDbBootstrapper
    {
        public void Setup()
        {
            //register all the automapper profiles
            Mapper.Initialize(cfg => cfg.AddProfiles(typeof(ClientMapperProfile)));
        }
    }
}
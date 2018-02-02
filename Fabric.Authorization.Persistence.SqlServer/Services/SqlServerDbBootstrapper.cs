using System;
using System.Linq;
using AutoMapper;
using Fabric.Authorization.Domain.Constants;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.SqlServer.Mappers;

namespace Fabric.Authorization.Persistence.SqlServer.Services
{
    public class SqlServerDbBootstrapper : IDbBootstrapper
    {
        private readonly IAuthorizationDbContext _authorizationDbContext;

        static SqlServerDbBootstrapper()
        {
            //register all the automapper profiles - only ever want to call this once
            //http://automapper.readthedocs.io/en/latest/Configuration.html#assembly-scanning-for-auto-configuration
            Mapper.Initialize(cfg => cfg.AddProfiles(typeof(SqlServerDbBootstrapper)));
           // Mapper.AssertConfigurationIsValid();            
        }

        public SqlServerDbBootstrapper(IAuthorizationDbContext authorizationDbContext)
        {
            _authorizationDbContext = authorizationDbContext;
        }

        public void Setup()
        {
            var grains = _authorizationDbContext.Grains.ToList();
            foreach (var builtInGrain in Grains.BuiltInGrains)
            {
                var existingGrain = grains.FirstOrDefault(g => g.Name == builtInGrain.Name);
                var incomingGrain = builtInGrain.ToEntity();
                if (existingGrain == null)
                {
                    incomingGrain.GrainId = Guid.NewGuid();
                    _authorizationDbContext.Grains.Add(incomingGrain);
                }
                else
                {
                    if (existingGrain.IsShared != incomingGrain.IsShared ||
                        existingGrain.IsDeleted != incomingGrain.IsDeleted ||
                        existingGrain.RequiredWriteScopes != incomingGrain.RequiredWriteScopes)
                    {
                        existingGrain.IsShared = incomingGrain.IsShared;
                        existingGrain.IsDeleted = incomingGrain.IsDeleted;
                        existingGrain.RequiredWriteScopes = incomingGrain.RequiredWriteScopes;
                    }
                }
                
            }
            _authorizationDbContext.SaveChanges();
        }
    }
}
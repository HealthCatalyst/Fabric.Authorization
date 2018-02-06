using System;
using System.Linq;
using System.Runtime.CompilerServices;
using AutoMapper;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Mappers;

namespace Fabric.Authorization.Persistence.SqlServer.Services
{
    public class SqlServerDbBootstrapper : IDbBootstrapper
    {
        private readonly IAuthorizationDbContext _authorizationDbContext;
        private readonly Domain.Defaults.Authorization _authorizationDefaults;

        static SqlServerDbBootstrapper()
        {
            //register all the automapper profiles - only ever want to call this once
            //http://automapper.readthedocs.io/en/latest/Configuration.html#assembly-scanning-for-auto-configuration
            Mapper.Initialize(cfg => cfg.AddProfiles(typeof(SqlServerDbBootstrapper)));
           // Mapper.AssertConfigurationIsValid();            
        }

        public SqlServerDbBootstrapper(IAuthorizationDbContext authorizationDbContext, Domain.Defaults.Authorization authorizationDefaults)
        {
            _authorizationDbContext = authorizationDbContext;
            _authorizationDefaults = authorizationDefaults;
        }

        public void Setup()
        {
            SetupGrains();
            _authorizationDbContext.SaveChanges();
        }

        private void SetupGrains()
        {
            var grains = _authorizationDbContext.Grains;
            foreach (var builtInGrain in _authorizationDefaults.Grains)
            {
                var existingGrain = grains.FirstOrDefault(g => g.Name == builtInGrain.Name);
                var incomingGrain = builtInGrain.ToEntity();
                if (existingGrain == null)
                {
                    incomingGrain.GrainId = Guid.NewGuid();
                    _authorizationDbContext.Grains.Add(incomingGrain);
                    SetupRoles(incomingGrain);
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
        }

        private void SetupRoles(Grain grain)
        {
            var roles = _authorizationDbContext.Roles;
            foreach (var defaultRole in _authorizationDefaults.Roles)
            {
                var existingRole = roles.FirstOrDefault(r => r.Name == defaultRole.Name);
                var securableItem = grain.SecurableItems.FirstOrDefault(si => si.Name == defaultRole.SecurableItem && si.Grain.Name == defaultRole.Grain);
                var incomingRole = defaultRole.ToEntity();
                if (existingRole == null && securableItem != null)
                {
                    incomingRole.RoleId = Guid.NewGuid();
                    incomingRole.SecurableItemId = securableItem.SecurableItemId;
                    _authorizationDbContext.Roles.Add(incomingRole);
                    SetupPermissions(defaultRole, securableItem, incomingRole);
                }
            }
        }

        private void SetupPermissions(Domain.Models.Role defaultRole, SecurableItem securableItem, Role incomingRole)
        {
            foreach (var permission in defaultRole.Permissions)
            {
                var existingPermission =
                    _authorizationDbContext.Permissions.FirstOrDefault(
                        p => p.Name == permission.Name && p.Grain == permission.Grain &&
                             p.SecurableItem.Name == securableItem.Name);
                if (existingPermission == null)
                {
                    var incomingPermission = permission.ToEntity();
                    incomingPermission.PermissionId = Guid.NewGuid();
                    incomingPermission.SecurableItemId = securableItem.SecurableItemId;
                    incomingPermission.RolePermissions.Add(new RolePermission { RoleId = incomingRole.RoleId, PermissionId = incomingPermission.PermissionId, PermissionAction = PermissionAction.Allow });
                    _authorizationDbContext.Permissions.Add(incomingPermission);
                }
            }
        }
    }
}
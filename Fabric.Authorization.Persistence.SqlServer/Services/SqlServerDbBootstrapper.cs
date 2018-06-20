using System;
using System.Linq;
using AutoMapper;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Microsoft.EntityFrameworkCore;

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
                var existingGrain = grains.Include(g => g.SecurableItems)
                    .FirstOrDefault(g => g.Name == builtInGrain.Name);

                var incomingGrain = builtInGrain.ToEntity();
                if (existingGrain == null)
                {
                    incomingGrain.GrainId = Guid.NewGuid();
                    _authorizationDbContext.Grains.Add(incomingGrain);
                    SetupRoles(incomingGrain);
                }
                else
                {
                    // do no apply updates to non-shared grains (e.g., app)
                    if (!existingGrain.IsShared)
                    {
                        continue;
                    }

                    // no changes occurred
                    if (existingGrain.IsDeleted == incomingGrain.IsDeleted &&
                        existingGrain.RequiredWriteScopes == incomingGrain.RequiredWriteScopes ||
                        existingGrain.SecurableItems.Count == incomingGrain.SecurableItems.Count)
                    {
                        continue;
                    }

                    existingGrain.IsDeleted = incomingGrain.IsDeleted;
                    existingGrain.RequiredWriteScopes = incomingGrain.RequiredWriteScopes;

                    foreach (var incomingSecurableItem in incomingGrain.SecurableItems)
                    {
                        var existingSecurableItem =
                            existingGrain.SecurableItems.FirstOrDefault(
                                si => si.Name == incomingSecurableItem.Name);

                        if (existingSecurableItem == null)
                        {
                            existingGrain.SecurableItems.Add(incomingSecurableItem);
                        }
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
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Resolvers.Permissions
{
    public interface IPermissionResolver
    {
        IEnumerable<Permission> Resolve(PermissionResolutionRequest resolutionRequest);
    }
}
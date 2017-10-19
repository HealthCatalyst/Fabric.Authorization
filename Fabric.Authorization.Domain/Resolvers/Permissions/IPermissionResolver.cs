using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Resolvers.Permissions
{
    public interface IPermissionResolver
    {
        Task<IEnumerable<Permission>> Resolve(PermissionResolutionRequest resolutionRequest);
    }
}
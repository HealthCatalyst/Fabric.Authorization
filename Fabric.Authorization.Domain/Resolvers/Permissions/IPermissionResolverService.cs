using System.Threading.Tasks;
using Fabric.Authorization.Domain.Resolvers.Models;

namespace Fabric.Authorization.Domain.Resolvers.Permissions
{
    public interface IPermissionResolverService
    {
        Task<PermissionResolutionResult> Resolve(PermissionResolutionRequest resolutionRequest);
    }
}
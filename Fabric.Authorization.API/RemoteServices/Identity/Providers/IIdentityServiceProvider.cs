using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.API.RemoteServices.Identity.Models;

namespace Fabric.Authorization.API.RemoteServices.Identity.Providers
{
    public interface IIdentityServiceProvider
    {
        Task<IEnumerable<UserSearchResponse>> Search(string clientId, IEnumerable<string> subjectIds);
    }
}
using Fabric.Authorization.API.Models.External.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.Services.External.Identity
{
    public interface IIdentityServiceProvider
    {
        Task<IEnumerable<IdentityUserSearchResponse>> Search(IEnumerable<string> subjectIds);
    }
}
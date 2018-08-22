using Fabric.Authorization.API.Models.EDW;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IEDWStore
    {
        void AddIdentitiesToRole(string[] identities, string roleName);
        void RemoveIdentitiesFromRole(string[] identities, string roleName);
    }
}
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IGroupStore
    {
        Group GetGroup(string groupName);

        IEnumerable<Group> GetAllGroups();

        Group AddGroup(Group group);

        Group DeleteGroup(string groupName);

        bool GroupExists(string groupName);
    }
}

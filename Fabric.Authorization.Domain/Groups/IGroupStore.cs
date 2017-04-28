using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Groups
{
    public interface IGroupStore
    {
        Group GetGroup(string groupName);
    }
}

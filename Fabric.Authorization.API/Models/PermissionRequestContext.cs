using System;
using System.Collections.Generic;

namespace Fabric.Authorization.API.Models
{
    public class PermissionRequestContext
    {
        public string RequestedGrain { get; set; }
        public string RequestedSecurableItem { get; set; }
    }

    public class PermissionRequestContextComparer : IEqualityComparer<PermissionRequestContext>
    {
        public bool Equals(PermissionRequestContext p1, PermissionRequestContext p2)
        {
            return string.Equals(p1.RequestedGrain, p2.RequestedGrain)
                   && string.Equals(p1.RequestedSecurableItem, p2.RequestedSecurableItem);
        }

        public int GetHashCode(PermissionRequestContext permissionRequestContext)
        {
            var hash = 13;
            hash = (hash * 7) + permissionRequestContext.RequestedGrain.GetHashCode();
            hash = (hash * 7) + permissionRequestContext.RequestedSecurableItem.GetHashCode();
            return hash;
        }
    }
}
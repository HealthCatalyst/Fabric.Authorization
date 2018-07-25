using Fabric.Authorization.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fabric.Authorization.UnitTests.Grains
{
    public static class SecurableItemExtension
    {
        public static bool IsSecurableItemChildOfGrain(this Grain grain, string securableItemName)
        {
            foreach (var securableItem in grain.SecurableItems)
            {
                if (HasRequestedSecurableItem(securableItem, securableItemName))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasRequestedSecurableItem(this SecurableItem parentSecurableItem, string securableItem)
        {
            if (parentSecurableItem.Name == securableItem)
            {
                return true;
            }
            var childSecurableItems = parentSecurableItem.SecurableItems;
            if (childSecurableItems == null || childSecurableItems.Count == 0)
            {
                return false;
            }

            if (childSecurableItems.Any(si => si.Name == securableItem))
            {
                return true;
            }

            return childSecurableItems.Any(
                childSecurableItem => HasRequestedSecurableItem(childSecurableItem, securableItem));
        }

    }
}

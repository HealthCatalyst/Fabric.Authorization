using System;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Services
{
    public interface ISecurableItemService
    {
        SecurableItem GetSecurableItem(string clientId, Guid itemId);
        SecurableItem GetTopLevelSecurableItem(string clientId);

        SecurableItem AddSecurableItem(string clientId, SecurableItem item);

        SecurableItem AddSecurableItem(string clientId, Guid itemId, SecurableItem item);
    }
}

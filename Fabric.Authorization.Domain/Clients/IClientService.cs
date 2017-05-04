using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Clients
{
    public interface IClientService
    {
        bool DoesClientOwnResource(string clientId, string grain, string resource);
    }
}

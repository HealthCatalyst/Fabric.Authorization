using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Clients
{
    public interface IClientStore
    {
        Client GetClient(string clientId);
    }
}

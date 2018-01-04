using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Persistence.SqlServer.Configuration
{
    public class ConnectionStrings : IConnectionStrings
    {
        public string AuthorizationDatabase { get; set; }
    }
}

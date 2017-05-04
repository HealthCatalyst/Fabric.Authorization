using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain
{
    public class Client
    {
        public string Id { get; set; }
        public Resource TopLevelResource { get; set; }
    }
}

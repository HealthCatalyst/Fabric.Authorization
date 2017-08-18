using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.Configuration
{
    public class HostingOptions
    {
        public bool UseIis { get; set; }
        public bool UseInMemoryStores { get; set; }

        public bool UseTestUsers { get; set; }
    }
}

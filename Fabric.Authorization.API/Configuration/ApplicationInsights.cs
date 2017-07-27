using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.Configuration
{
    public class ApplicationInsights
    {
        public string InstrumentationKey { get; set; }
        public bool Enabled { get; set; }
    }
}

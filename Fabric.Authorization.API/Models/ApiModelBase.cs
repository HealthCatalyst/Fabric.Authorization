using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.Models
{
    public class ApiModelBase
    {
        public string UserId { get; set; }
        public string RequestedGrain { get; set; }
        public string RequestedResource { get; set; }
    }
}

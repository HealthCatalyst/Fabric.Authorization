using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.Models
{
    public class GroupInfoRequest
    {
        public string GroupName { get; set; }
        public string Grain { get; set; }
        public string Resource { get; set; }
    }
}

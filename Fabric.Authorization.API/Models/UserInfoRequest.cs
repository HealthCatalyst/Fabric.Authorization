using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.Models
{
    public class UserInfoRequest
    {
        public string Grain { get; set; }
        public string SecurableItem { get; set; }
    }
}

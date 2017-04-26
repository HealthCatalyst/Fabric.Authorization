using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.Models
{
    public class UserRoleResponse : ResponseBase
    {
        public IEnumerable<string> Roles { get; set; }
    }
}

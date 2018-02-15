using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.Models
{
    public class RoleUserRequest
    {
        public string SubjectId { get; set; }
        public string IdentityProvider { get; set; }
    }
}

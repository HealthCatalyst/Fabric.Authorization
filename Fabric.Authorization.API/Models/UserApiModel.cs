using System.Collections.Generic;

namespace Fabric.Authorization.API.Models
{
    public class UserApiModel
    {
        public string UserId { get; set; }

        public IEnumerable<string> Groups { get; set; }
    }
}
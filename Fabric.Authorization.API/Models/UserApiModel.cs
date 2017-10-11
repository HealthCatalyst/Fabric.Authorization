using System.Collections.Generic;

namespace Fabric.Authorization.API.Models
{
    public class UserApiModel
    {
        public string SubjectId { get; set; }

        public string IdentityProvider { get; set; }

        public IEnumerable<string> Groups { get; set; }
    }
}
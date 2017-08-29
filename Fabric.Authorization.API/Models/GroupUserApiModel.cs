using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.API.Models
{
    public class GroupUserApiModel : IIdentifiable
    {
        public string Id { get; set; }

        public string Identifier => Id;

        public string GroupName { get; set; }

        public string GroupSource { get; set; }

        public IEnumerable<UserApiModel> Users { get; set; }
    }
}
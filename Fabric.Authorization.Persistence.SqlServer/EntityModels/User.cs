using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class User : ITrackable, ISoftDelete
    {
        public User()
        {
            UserGroups = new List<UserGroup>();
            UserPermissions = new List<UserPermission>();
        }

        public int Id { get; set; }
        public string SubjectId { get; set; }
        public string IdentityProvider { get; set; }
        public string Name { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }

        public ICollection<UserGroup> UserGroups { get; set; }
        public ICollection<UserPermission> UserPermissions { get; set; }
    }
}
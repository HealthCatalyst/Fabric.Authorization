using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class User : IUser, ITrackable, ISoftDelete
    {
        public User()
        {
            GroupUsers = new List<GroupUser>();
            UserPermissions = new List<UserPermission>();
            RoleUsers = new List<RoleUser>();
        }

        public int Id { get; set; }
        public string IdentityProvider { get; set; }
        public string SubjectId { get; set; }
        public string Name { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }

        public string ComputedUserId { get; set; }

        public ICollection<GroupUser> GroupUsers { get; set; }
        public ICollection<UserPermission> UserPermissions { get; set; }
        public ICollection<RoleUser> RoleUsers { get; set; }
        
    }
}
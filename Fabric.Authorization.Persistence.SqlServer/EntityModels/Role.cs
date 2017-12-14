using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class Role : ITrackable, ISoftDelete
    {
        public Role()
        {
            Groups = new List<Group>();
            ChildRoles = new List<Role>();
            /*Permissions = new List<Permission>();
            DeniedPermissions = new List<Permission>();*/
        }
        public int Id { get; set; }
        public int? ParentRoleId { get; set; }
        public Guid ExternalIdentifier { get; set; }
        public string Name { get; set; }
        public string Grain { get; set; }
        public string SecurableItem { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }

        public ICollection<Group> Groups { get; set; }
        public ICollection<Role> ChildRoles { get; set; }

        /*public ICollection<Permission> Permissions { get; set; }
        public ICollection<Permission> DeniedPermissions { get; set; }*/
    }
}
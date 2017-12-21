using System;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class RolePermission : ITrackable, ISoftDelete
    {
        public int Id { get; set; }
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }
        public PermissionAction PermissionAction { get; set; }

        public bool IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string ModifiedBy { get; set; }

        public Role Role { get; set; }
        public Permission Permission { get; set; }
    }
}
using System;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public enum PermissionAction
    {
        Allow,
        Deny
    }

    public class RolePermission : ITrackable, ISoftDelete
    {
        public PermissionAction PermissionAction { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
using System;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class UserPermission : ITrackable, ISoftDelete
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public Guid PermissionId { get; set; }
        public PermissionAction PermissionAction { get; set; }

        public bool IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string ModifiedBy { get; set; }

        public User User { get; set; }
        public Permission Permission { get; set; }
    }
}
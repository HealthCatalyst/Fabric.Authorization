using System;
using System.Collections.Generic;

namespace Catalyst.Fabric.Authorization.Models
{
    public class RoleApiModel : ITrackable, IIdentifiable<Guid?>
    {
        public Guid? Id { get; set; }

        public string Grain { get; set; }

        public string SecurableItem { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public Guid? ParentRole { get; set; }

        public IEnumerable<PermissionApiModel> Permissions { get; set; }

        public IEnumerable<PermissionApiModel> DeniedPermissions { get; set; }

        public IEnumerable<Guid> ChildRoles { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        public DateTime? ModifiedDateTimeUtc { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }

        public override string ToString()
        {
            return $"{Grain}/{SecurableItem}.{Name}";
        }
    }
}
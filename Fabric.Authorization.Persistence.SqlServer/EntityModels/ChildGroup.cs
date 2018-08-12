using System;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class ChildGroup
    {
        public int Id { get; set; }
        public Guid ParentId { get; set; }
        public Guid ChildId { get; set; }

        public bool IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string ModifiedBy { get; set; }

        public Group Parent { get; set; }
        public Group Child { get; set; }
    }
}

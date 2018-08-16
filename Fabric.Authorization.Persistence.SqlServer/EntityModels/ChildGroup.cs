using System;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class ChildGroup : ITrackable, ISoftDelete
    {
        public int Id { get; set; }
        public Guid ParentGroupId { get; set; }
        public Guid ChildGroupId { get; set; }

        public bool IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string ModifiedBy { get; set; }

        public Group Parent { get; set; }
        public Group Child { get; set; }
    }
}

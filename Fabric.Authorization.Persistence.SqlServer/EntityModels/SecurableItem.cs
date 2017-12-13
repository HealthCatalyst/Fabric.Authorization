using System;
using System.Collections.Generic;
using System.Text;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class SecurableItem : ITrackable, ISoftDelete
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }


    }
}

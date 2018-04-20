using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Events
{
    public class EntityBatchAuditEvent<T> : Event
    {
        public IEnumerable<T> Entities { get; set; }

        public EntityBatchAuditEvent(string name, IEnumerable<T> entities) : base(name)
        {
            Entities = entities;
        }
    }
}

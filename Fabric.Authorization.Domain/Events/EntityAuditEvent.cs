namespace Fabric.Authorization.Domain.Events
{
    public class EntityAuditEvent<T> : Event
    {
        public EntityAuditEvent(string name, string entityId) : base(name)
        {
            EntityId = entityId;
            EntityType = typeof(T).FullName;
        }

        public EntityAuditEvent(string name, string entityId, T entity) : this(name, entityId)
        {
            Entity = entity;
        }

        public string EntityId { get; set; }
        public string EntityType { get; set; }
        public T Entity { get; set; }
    }
}
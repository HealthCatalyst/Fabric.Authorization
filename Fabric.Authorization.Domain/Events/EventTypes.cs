namespace Fabric.Authorization.Domain.Events
{
    public static class EventTypes
    {
        public static readonly string EntityCreatedEvent = "EntityCreated";
        public static readonly string ChildEntityCreatedEvent = "ChildEntityCreated";
        public static readonly string EntityUpdatedEvent = "EntityUpdated";
        public static readonly string EntityBatchUpdatedEvent = "EntityBatchUpdated";
        public static readonly string EntityDeletedEvent = "EntityDeleted";
        public static readonly string ChildEntityDeletedEvent = "ChildEntityDeleted";
        public static readonly string EntityReadEvent = "EntityRead";
    }
}

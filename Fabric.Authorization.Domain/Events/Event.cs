using System;

namespace Fabric.Authorization.Domain.Events
{
    public abstract class Event
    {
        protected Event(string name)
        {
            Name = name;
        }

        public DateTime Timestamp { get; set; }
        public string Username { get; set; }
        public string ClientId { get; set; }
        public string Subject { get; set; }
        public string Name { get; set; }
        public string RemoteIpAddress { get; set; }


    }
}

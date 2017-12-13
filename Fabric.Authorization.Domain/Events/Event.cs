using System;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Events
{
    public abstract class Event : IIdentifiable
    {
        protected Event(string name)
        {
            Identifier = new Guid().ToString();
            Name = name;
        }

        public string Identifier { get; set; }
        public DateTime Timestamp { get; set; }
        public string Username { get; set; }
        public string ClientId { get; set; }
        public string Subject { get; set; }
        public string Name { get; set; }
        public string RemoteIpAddress { get; set; }
    }
}

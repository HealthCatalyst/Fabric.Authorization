using System;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class GroupNotFoundException : Exception
    {
        public GroupNotFoundException()
        {
        }

        public GroupNotFoundException(string message) : base(message)
        {
        }

        public GroupNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }


    }
}

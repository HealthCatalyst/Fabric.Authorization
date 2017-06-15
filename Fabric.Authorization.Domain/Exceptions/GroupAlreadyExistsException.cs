using System;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class GroupAlreadyExistsException : Exception
    {
        public GroupAlreadyExistsException()
        {
        }

        public GroupAlreadyExistsException(string message) : base(message)
        {
        }

        public GroupAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

using System;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class SecurableItemAlreadyExistsException : Exception
    {
        public SecurableItemAlreadyExistsException()
        {
        }

        public SecurableItemAlreadyExistsException(string message) : base(message)
        {
        }

        public SecurableItemAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

using System;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class SecurableItemNotFoundException : Exception
    {
        public SecurableItemNotFoundException()
        {
        }

        public SecurableItemNotFoundException(string message) : base(message)
        {
        }

        public SecurableItemNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

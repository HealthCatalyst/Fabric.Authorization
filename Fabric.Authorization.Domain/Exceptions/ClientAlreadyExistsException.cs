using System;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class ClientAlreadyExistsException : Exception
    {
        public ClientAlreadyExistsException()
        {
        }

        public ClientAlreadyExistsException(string message) : base(message)
        {
        }

        public ClientAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

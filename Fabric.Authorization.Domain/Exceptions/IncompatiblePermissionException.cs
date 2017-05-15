using System;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class IncompatiblePermissionException : Exception
    {
        public IncompatiblePermissionException()
        {
        }

        public IncompatiblePermissionException(string message) : base(message)
        {
        }

        public IncompatiblePermissionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

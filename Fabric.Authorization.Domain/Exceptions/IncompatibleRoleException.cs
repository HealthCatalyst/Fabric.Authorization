using System;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class IncompatibleRoleException : Exception
    {
        public IncompatibleRoleException()
        {
        }

        public IncompatibleRoleException(string message) : base(message)
        {
        }

        public IncompatibleRoleException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

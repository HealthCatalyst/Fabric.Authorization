using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class RoleAlreadyExistsException : Exception
    {
        public RoleAlreadyExistsException()
        {
        }

        public RoleAlreadyExistsException(string message) : base(message)
        {
        }

        public RoleAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

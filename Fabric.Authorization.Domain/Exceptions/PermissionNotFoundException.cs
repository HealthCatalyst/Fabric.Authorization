using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Exceptions
{
    public class PermissionNotFoundException : Exception
    {
        public PermissionNotFoundException()
        {
        }

        public PermissionNotFoundException(string message) : base(message)
        {
        }

        public PermissionNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

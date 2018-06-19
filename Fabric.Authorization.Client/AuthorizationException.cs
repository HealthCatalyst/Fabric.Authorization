namespace Fabric.Authorization.Client
{
    using System;
    using Fabric.Authorization.Models;

    public class AuthorizationException : Exception
    {
        public AuthorizationException(Error errorMessage)
        {
            throw new NotImplementedException();
        }
    }
}

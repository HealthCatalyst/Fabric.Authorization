using System;
using Fabric.Authorization.Models;

namespace Fabric.Authorization.Client
{
    public class AuthorizationException : Exception
    {
        public AuthorizationException(Error errorMessage)
        {

        }
    }
}
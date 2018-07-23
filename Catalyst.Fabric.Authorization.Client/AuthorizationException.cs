using System;
using Catalyst.Fabric.Authorization.Models;

namespace Catalyst.Fabric.Authorization.Client
{
    public class AuthorizationException : Exception
    {
        public Error Details { get; set; }

        public AuthorizationException(Error errorMessage) 
            : base(errorMessage.Message)
        {
            this.Details = errorMessage;
        }
    }
}
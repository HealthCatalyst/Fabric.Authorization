using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IDbBootstrapper
    {
        void Setup();
    }
}

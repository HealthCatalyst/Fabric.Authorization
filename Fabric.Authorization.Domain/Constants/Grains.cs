using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Constants
{
    public static class Grains
    {
        public static IList<Grain> BuiltInGrains = new List<Grain>
        {
            new Grain
            {
                Name = "app"
            },
            new Grain
            {
                Name = "dos",
                IsShared = true,
                RequiredWriteScopes = new List<string> { "fabric/authorization.dos.write" }
            }
        };
    }
}

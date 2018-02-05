using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Defaults
{
    public class Authorization
    {
        public IList<Grain> Grains = new List<Grain>
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

using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Defaults
{
    public class Authorization
    {
        public IList<Grain> Grains { get; }
        public IList<Role> Roles { get; }

        public Authorization()
        {
            Grains = new List<Grain>
            {
                new Grain
                {
                    Name = "app"
                },
                new Grain
                {
                    Name = "dos",
                    IsShared = true,
                    RequiredWriteScopes = new List<string> { "fabric/authorization.dos.write" },
                    SecurableItems = new List<SecurableItem>
                    {
                        new SecurableItem
                        {
                            Name = "datamarts",
                            ClientOwner = "metadata-service"
                        }
                    }
                }
            };

            Roles = new List<Role>
            {
                new Role
                {
                    Name = "dosadmin",
                    Grain = "dos",
                    SecurableItem = "datamarts",
                    Permissions = new List<Permission>
                    {
                        new Permission
                        {
                            Name = "manageauthorization",
                            Grain = "dos",
                            SecurableItem = "datamarts"
                        }
                    }
                }
            };
        }
    }
}

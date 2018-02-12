using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Defaults
{
    public class Authorization
    {
        public static string AppGrain = "app";
        public static string DosGrain = "dos";
        public static string AuthorizationPermissionName = "manageauthorization";
        public static string InstallerClientId = "fabric-installer";

        public IList<Grain> Grains { get; }
        public IList<Role> Roles { get; }

        public Authorization()
        {
            Grains = new List<Grain>
            {
                new Grain
                {
                    Name = AppGrain
                },
                new Grain
                {
                    Name = DosGrain,
                    IsShared = true,
                    RequiredWriteScopes = new List<string> { "fabric/authorization.dos.write" },
                    SecurableItems = new List<SecurableItem>
                    {
                        new SecurableItem
                        {
                            Name = "datamarts",
                            ClientOwner = "dos-metadata-service"
                        }
                    }
                }
            };

            Roles = new List<Role>
            {
                new Role
                {
                    Name = "dosadmin",
                    Grain = DosGrain,
                    SecurableItem = "datamarts",
                    Permissions = new List<Permission>
                    {
                        new Permission
                        {
                            Name = AuthorizationPermissionName,
                            Grain = "dos",
                            SecurableItem = "datamarts"
                        }
                    }
                }
            };
        }
    }
}

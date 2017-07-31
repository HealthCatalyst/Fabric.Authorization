using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Fabric.Authorization.API;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Nancy;
using Nancy.Testing;
using Serilog;
using Group = Fabric.Authorization.Domain.Models.Group;

namespace Fabric.Authorization.UnitTests
{
    public abstract class ModuleTestsBase<T> where T : NancyModule
    {
        protected readonly List<Client> ExistingClients;
        protected readonly List<Role> ExistingRoles;
        protected readonly List<Group> ExistingGroups;
        protected readonly List<Permission> ExistingPermissions;
        protected readonly Mock<ILogger> MockLogger;
        protected readonly Mock<IPermissionStore> MockPermissionStore;
        protected readonly Mock<IRoleStore> MockRoleStore;
        protected readonly Mock<IGroupStore> MockGroupStore;
        protected readonly Mock<IClientStore> MockClientStore;

        protected ModuleTestsBase()
        {
            this.ExistingClients = CreateClients();
            this.ExistingRoles = CreateRoles();
            this.ExistingGroups = CreateGroups();
            this.ExistingPermissions = CreatePermissions();
            this.MockLogger = new Mock<ILogger>();
            this.MockClientStore = new Mock<IClientStore>()
                .SetupGetClient(ExistingClients)
                .SetupAddClient();

            this.MockPermissionStore = new Mock<IPermissionStore>()
                .SetupGetPermissions(ExistingPermissions)
                .SetupAddPermissions();

            this.MockRoleStore = new Mock<IRoleStore>()
                .SetupGetRoles(ExistingRoles)
                .SetupAddRole();

            this.MockGroupStore = new Mock<IGroupStore>()
                .SetupGetGroups(ExistingGroups)
                .SetupAddGroup();
        }

        protected Browser CreateBrowser(params Claim[] claims)
        {
            return new Browser(CreateBootstrapper(claims), withDefaults =>
            {
                withDefaults.Accept("application/json");
                withDefaults.HostName("testhost");

            });
        }

        private ConfigurableBootstrapper CreateBootstrapper(params Claim[] claims)
        {
            var configurableBootstrapper = new ConfigurableBootstrapper();
            ConfigureBootstrapper(configurableBootstrapper, claims);
            return configurableBootstrapper;
        }

        protected virtual ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator ConfigureBootstrapper(ConfigurableBootstrapper configurableBootstrapper, params Claim[] claims)
        {
            var configurableBootstrapperConfigurator = new ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator(configurableBootstrapper);
            configurableBootstrapperConfigurator.Module<T>();
            configurableBootstrapperConfigurator.RequestStartup((container, pipeline, context) =>
            {
                context.CurrentUser = new TestPrincipal(claims);
                pipeline.BeforeRequest += (ctx) => RequestHooks.SetDefaultVersionInUrl(ctx); ;
            });
            return configurableBootstrapperConfigurator;
        }

        private List<Client> CreateClients()
        {
            return new List<Client>
            {
                new Client
                {
                    Id = "patientsafety",
                    TopLevelSecurableItem = new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Name = "patientsafety"
                    }
                },
                new Client
                {
                    Id = "sourcemartdesigner",
                    TopLevelSecurableItem = new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Name = "sourcemartdesigner"
                    }
                }
            };
        }

        private List<Role> CreateRoles()
        {
            return new List<Role>
            {
                new Role
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    SecurableItem = "patientsafety",
                    Name = "admin"
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    SecurableItem = "sourcemartdesigner",
                    Name = "admin"
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    SecurableItem = "sourcemartdesigner",
                    Name = "manager",
                    IsDeleted = true
                }
            };
        }

        private List<Group> CreateGroups()
        {
            return new List<Group>
            {
                new Group
                {
                    Id = Guid.NewGuid().ToString(),
                    IsDeleted = false,
                    Roles = this.CreateRoles()
                },
                new Group
                {
                    Id = Guid.NewGuid().ToString(),
                    IsDeleted = false,
                    Roles = this.CreateRoles()
                }
            };
        }

        private List<Permission> CreatePermissions()
        {
            return new List<Permission>
            {
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    SecurableItem = "patientsafety",
                    Name = "manageusers"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    SecurableItem = "patientsafety",
                    Name = "updatepatient"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    SecurableItem = "sourcemartdesigner",
                    Name = "manageusers"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Grain = "patient",
                    SecurableItem = "Patient",
                    Name ="read"
                }
            };
        }
       
    }
}
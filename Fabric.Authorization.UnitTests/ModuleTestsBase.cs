using System;
using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Nancy;
using Nancy.Testing;
using Serilog;

namespace Fabric.Authorization.UnitTests
{
    public abstract class ModuleTestsBase<T> where T : NancyModule
    {
        protected readonly List<Client> ExistingClients;
        protected readonly List<Group> ExistingGroups;
        protected readonly List<Permission> ExistingPermissions;
        protected readonly List<Role> ExistingRoles;
        protected readonly Mock<IClientStore> MockClientStore;
        protected readonly Mock<IGroupStore> MockGroupStore;
        protected readonly Mock<ILogger> MockLogger;
        protected readonly Mock<IPermissionStore> MockPermissionStore;
        protected readonly Mock<IRoleStore> MockRoleStore;
        protected readonly Mock<IUserStore> MockUserStore;
        protected readonly Mock<IGrainStore> MockGrainStore;

        protected ModuleTestsBase()
        {
            ExistingClients = CreateClients;
            ExistingRoles = CreateRoles;
            ExistingGroups = CreateGroups;
            ExistingPermissions = CreatePermissions;
            MockLogger = new Mock<ILogger>();
            MockClientStore = new Mock<IClientStore>()
                .SetupGetClient(ExistingClients)
                .SetupAddClient();

            MockPermissionStore = new Mock<IPermissionStore>()
                .SetupGetPermissions(ExistingPermissions)
                .SetupAddPermissions(ExistingPermissions)
                .SetupGetGranularPermissions();

            MockRoleStore = new Mock<IRoleStore>()
                .SetupGetRoles(ExistingRoles)
                .SetupAddRole(ExistingRoles);

            MockGroupStore = new Mock<IGroupStore>()
                .SetupGetGroups(ExistingGroups)
                .SetupAddGroup();

            MockGrainStore = new Mock<IGrainStore>();

            MockUserStore = new Mock<IUserStore>();
        }

        private List<Client> CreateClients => new List<Client>
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

        private List<Role> CreateRoles => new List<Role>
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

        private List<Group> CreateGroups => new List<Group>
        {
            new Group
            {
                Id = Guid.NewGuid().ToString(),
                IsDeleted = false,
                Roles = CreateRoles
            },
            new Group
            {
                Id = Guid.NewGuid().ToString(),
                IsDeleted = false,
                Roles = CreateRoles
            }
        };

        private List<Permission> CreatePermissions => new List<Permission>
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
                Name = "read"
            }
        };

        protected Browser CreateBrowser(params Claim[] claims)
        {
            return new Browser(CreateBootstrapper(claims), withDefaults =>
            {
                withDefaults.Accept("application/json");                
                withDefaults.HostName("testhost");
                withDefaults.Header("Content-Type", "application/json");
            });
        }

        private ConfigurableBootstrapper CreateBootstrapper(params Claim[] claims)
        {
            var configurableBootstrapper = new ConfigurableBootstrapper();
            ConfigureBootstrapper(configurableBootstrapper, claims);
            return configurableBootstrapper;
        }

        protected virtual ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator ConfigureBootstrapper(
            ConfigurableBootstrapper configurableBootstrapper, params Claim[] claims)
        {
            var configurableBootstrapperConfigurator =
                new ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator(configurableBootstrapper);
            configurableBootstrapperConfigurator.Module<T>();
            configurableBootstrapperConfigurator.RequestStartup((container, pipeline, context) =>
            {
                context.CurrentUser = new TestPrincipal(claims);
                pipeline.BeforeRequest += ctx => API.Infrastructure.PipelineHooks.RequestHooks.RemoveContentTypeHeaderForGet(ctx);
                pipeline.BeforeRequest += ctx => API.Infrastructure.PipelineHooks.RequestHooks.SetDefaultVersionInUrl(ctx);
            });
            return configurableBootstrapperConfigurator;
        }
    }
}
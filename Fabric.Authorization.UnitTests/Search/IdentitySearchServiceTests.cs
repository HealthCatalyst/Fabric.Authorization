using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.API.RemoteServices.Identity.Models;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Search
{
    [Collection("Identity Search Tests")]
    public class IdentitySearchServiceTests
    {
        public IdentitySearchServiceTests()
        {
            InitializeData();

            _mockClientStore.SetupGetClient(new List<Client> {_patientSafetyClient, _atlasClient});

            _mockRoleStore.SetupGetRoles(new List<Role>
            {
                _adminPatientSafetyRole,
                _userPatientSafetyRole,
                _adminAtlasRole,
                _userAtlasRole
            });

            _mockPermissionStore.SetupGetPermissions(new List<Permission>());

            _mockGroupStore.SetupGetGroups(new List<Group>
            {
                _adminPatientSafetyGroup,
                _userPatientSafetyGroup,
                _adminAtlasGroup,
                _userAtlasGroup
            });

            _clientService = new ClientService(_mockClientStore.Create());
            _roleService = new RoleService(_mockRoleStore.Create(), _mockPermissionStore.Create(), _clientService);
            _groupService = new GroupService(_mockGroupStore.Create(), _mockRoleStore.Create(),
                new Mock<IUserStore>().Object);
        }

        private void InitializeData()
        {
            InitializePatientSafetyData();
            InitializeAtlasData();
        }

        private void InitializePatientSafetyData()
        {
            _adminPatientSafetyRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = AdminPatientSafetyRoleName,
                Grain = "app",
                SecurableItem = "patientsafety",
                Groups = new List<string>
                {
                    AdminPatientSafetyGroupName
                }
            };

            _userPatientSafetyRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = UserPatientSafetyRoleName,
                Grain = "patientsafety",
                SecurableItem = "patient",
                Groups = new List<string>
                {
                    UserPatientSafetyGroupName
                }
            };

            _adminPatientSafetyGroup = new Group
            {
                Id = AdminPatientSafetyGroupName,
                Name = AdminPatientSafetyGroupName,
                Roles = new List<Role> {_adminPatientSafetyRole}
            };

            _userPatientSafetyGroup = new Group
            {
                Id = UserPatientSafetyGroupName,
                Name = UserPatientSafetyGroupName,
                Users = new List<User>
                {
                    new User
                    {
                        SubjectId = "patientsafety_user",
                        Groups = new List<string>{UserPatientSafetyGroupName}
                    }
                },
                Roles = new List<Role> {_userPatientSafetyRole},
                Source = "Custom"
            };
        }

        private void InitializeAtlasData()
        {
            _adminAtlasRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = AdminAtlasRoleName,
                Grain = "app",
                SecurableItem = "atlas",
                Groups = new List<string>
                {
                    AdminAtlasGroupName
                }
            };

            _userAtlasRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = UserAtlasRoleName,
                Grain = "atlas",
                SecurableItem = "atlas-si2",// "patient",
                Groups = new List<string>
                {
                    UserAtlasGroupName
                }
            };

            _adminAtlasGroup = new Group
            {
                Id = AdminAtlasGroupName,
                Name = AdminAtlasGroupName,
                Roles = new List<Role> { _adminAtlasRole }
            };

            _userAtlasGroup = new Group
            {
                Id = UserAtlasGroupName,
                Name = UserAtlasGroupName,
                Users = new List<User>
                {
                    new User
                    {
                        SubjectId = "atlas_user",
                        Groups = new List<string>{ UserAtlasGroupName }
                    }
                },
                Roles = new List<Role> { _userAtlasRole },
                Source = "Custom"
            };
        }

        private const string PatientSafetyClientId = "patientsafety";
        private const string AdminPatientSafetyGroupName = "adminPatientSafetyGroup";
        private const string UserPatientSafetyGroupName = "userPatientSafetyGroup";
        private const string AdminPatientSafetyRoleName = "adminPatientSafetyRole";
        private const string UserPatientSafetyRoleName = "userPatientSafetyRole";

        private readonly Client _patientSafetyClient = new Client
        {
            Id = PatientSafetyClientId,
            TopLevelSecurableItem = new SecurableItem
            {
                Id = Guid.NewGuid(),
                Name = "patientsafety",
                SecurableItems = new List<SecurableItem>
                {
                    new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Name = "patientsafety-si1",
                        SecurableItems = new List<SecurableItem>
                        {
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "patient"
                            },
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "diagnoses"
                            }
                        }
                    },
                    new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Name = "patientsafety-si2",
                        SecurableItems = new List<SecurableItem>
                        {
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "patient"
                            },
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "observations"
                            }
                        }
                    }
                }
            }
        };

        private const string AtlasClientId = "atlas";
        private const string AdminAtlasGroupName = "adminAtlasGroup";
        private const string UserAtlasGroupName = "userAtlasGroup";
        private const string AdminAtlasRoleName = "adminAtlasRole";
        private const string UserAtlasRoleName = "userAtlasRole";

        private readonly Client _atlasClient = new Client
        {
            Id = AtlasClientId,
            TopLevelSecurableItem = new SecurableItem
            {
                Id = Guid.NewGuid(),
                Name = "atlas",
                SecurableItems = new List<SecurableItem>
                {
                    new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Name = "atlas-si1",
                        SecurableItems = new List<SecurableItem>
                        {
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "patient"
                            },
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "diagnoses"
                            }
                        }
                    },
                    new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Name = "atlas-si2",
                        SecurableItems = new List<SecurableItem>
                        {
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "patient"
                            },
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "observations"
                            }
                        }
                    }
                }
            }
        };

        private Role _adminPatientSafetyRole;
        private Role _userPatientSafetyRole;
        private Group _adminPatientSafetyGroup;
        private Group _userPatientSafetyGroup;

        private Role _adminAtlasRole;
        private Role _userAtlasRole;
        private Group _adminAtlasGroup;
        private Group _userAtlasGroup;

        private readonly Mock<IClientStore> _mockClientStore = new Mock<IClientStore>();
        private readonly Mock<IPermissionStore> _mockPermissionStore = new Mock<IPermissionStore>();
        private readonly Mock<IRoleStore> _mockRoleStore = new Mock<IRoleStore>();
        private readonly Mock<IGroupStore> _mockGroupStore = new Mock<IGroupStore>();

        private readonly ClientService _clientService;
        private readonly RoleService _roleService;
        private readonly GroupService _groupService;

        [Fact]
        public async Task IdentitySearch_ClientIdMissing_BadRequestExceptionAsync()
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();

            var identitySearchService = new IdentitySearchService(
                _clientService,
                _roleService,
                _groupService,
                mockIdentityServiceProvider.Object);

            await Assert.ThrowsAsync<BadRequestException<IdentitySearchRequest>>(() => identitySearchService.Search(new IdentitySearchRequest()));
        }

        /*
INPUTS
client id | page number | page size | filter | sort key | sort direction
 */
        [Fact]
        public void IdentitySearch_ValidRequest_Success()
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            mockIdentityServiceProvider
                .Setup(m => m.Search(AtlasClientId, new List<string> {"atlas_user"}))
                .ReturnsAsync(() => new List<UserSearchResponse>
                {
                    new UserSearchResponse
                    {
                        SubjectId = "atlas_user",
                        FirstName = "Robert",
                        MiddleName = "Brian",
                        LastName = "Smith",
                        LastLoginDate = new DateTime(2017, 9, 15)
                    }
                });

            var identitySearchService = new IdentitySearchService(_clientService, _roleService, _groupService,
                mockIdentityServiceProvider.Object);

            var results = identitySearchService.Search(new IdentitySearchRequest
            {
                ClientId = AtlasClientId,
                SortKey = "name",
                SortDirection = "desc"
            }).Result.ToList();

            Assert.Equal(2, results.Count);
        }
    }
}
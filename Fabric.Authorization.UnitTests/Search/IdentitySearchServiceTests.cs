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
    public class IdentitySearchServiceFixture
    {
        private const string PatientSafetyClientId = "patientsafety";
        private const string AdminPatientSafetyGroupName = "adminPatientSafetyGroup";
        private const string UserPatientSafetyGroupName = "userPatientSafetyGroup";
        private const string AdminPatientSafetyRoleName = "adminPatientSafetyRole";
        private const string UserPatientSafetyRoleName = "userPatientSafetyRole";

        public const string AtlasClientId = "atlas";
        public const string AdminAtlasGroupName = "adminAtlasGroup";
        public const string UserAtlasGroupName = "userAtlasGroup";
        public const string AdminAtlasRoleName = "adminAtlasRole";
        public const string UserAtlasRoleName = "userAtlasRole";

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

        private readonly ClientService _clientService;
        private readonly GroupService _groupService;

        private readonly Mock<IClientStore> _mockClientStore = new Mock<IClientStore>();
        private readonly Mock<IGroupStore> _mockGroupStore = new Mock<IGroupStore>();
        private readonly Mock<IPermissionStore> _mockPermissionStore = new Mock<IPermissionStore>();
        private readonly Mock<IRoleStore> _mockRoleStore = new Mock<IRoleStore>();

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

        private readonly RoleService _roleService;
        private Group _adminAtlasGroup;

        private Role _adminAtlasRole;
        private Group _adminPatientSafetyGroup;

        private Role _adminPatientSafetyRole;
        private Group _userAtlasGroup;
        private Role _userAtlasRole;
        private Group _userPatientSafetyGroup;
        private Role _userPatientSafetyRole;

        public IdentitySearchServiceFixture()
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

        public IdentitySearchService IdentitySearchService(IIdentityServiceProvider identityServiceProvider)
        {
            var identitySearchService =
                new IdentitySearchService(_clientService, _roleService, _groupService, identityServiceProvider);
            return identitySearchService;
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
                Roles = new List<Role>
                {
                    new Role
                    {
                        Id = _adminPatientSafetyRole.Id,
                        Name = AdminPatientSafetyRoleName,
                        Grain = "app",
                        SecurableItem = "patientsafety",
                        Groups = new List<string>
                        {
                            AdminPatientSafetyGroupName
                        }
                    }
                },
                Source = "Windows"
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
                        Groups = new List<string> {UserPatientSafetyGroupName}
                    }
                },
                Roles = new List<Role>
                {
                    new Role
                    {
                        Id = _userPatientSafetyRole.Id,
                        Name = UserPatientSafetyRoleName,
                        Grain = "patientsafety",
                        SecurableItem = "patient",
                        Groups = new List<string>
                        {
                            UserPatientSafetyGroupName
                        }
                    }
                },
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
                SecurableItem = "atlas-si2",
                Groups = new List<string>
                {
                    UserAtlasGroupName
                }
            };

            _adminAtlasGroup = new Group
            {
                Id = AdminAtlasGroupName,
                Name = AdminAtlasGroupName,
                Roles = new List<Role>
                {
                    new Role
                    {
                        Id = _adminAtlasRole.Id,
                        Name = AdminAtlasRoleName,
                        Grain = "app",
                        SecurableItem = "atlas",
                        Groups = new List<string>
                        {
                            AdminAtlasGroupName
                        }
                    }
                }
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
                        Groups = new List<string> {UserAtlasGroupName}
                    }
                },
                Roles = new List<Role>
                {
                    new Role
                    {
                        Id = _userAtlasRole.Id,
                        Name = UserAtlasRoleName,
                        Grain = "atlas",
                        SecurableItem = "atlas-si2",
                        Groups = new List<string>
                        {
                            UserAtlasGroupName
                        }
                    }
                },
                Source = "Custom"
            };
        }
    }

    [Collection("Identity Search Tests")]
    public class IdentitySearchServiceTests : IClassFixture<IdentitySearchServiceFixture>
    {
        public IdentitySearchServiceTests(IdentitySearchServiceFixture fixture)
        {
            _fixture = fixture;
        }

        private readonly IdentitySearchServiceFixture _fixture;

        [Fact]
        public async Task IdentitySearch_ClientIdMissing_BadRequestExceptionAsync()
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            var identitySearchService = _fixture.IdentitySearchService(mockIdentityServiceProvider.Object);

            await Assert.ThrowsAsync<BadRequestException<IdentitySearchRequest>>(
                () => identitySearchService.Search(new IdentitySearchRequest()));
        }

        [Fact]
        public void IdentitySearch_ValidRequest_Success()
        {
            var lastLoginDate = new DateTime(2017, 9, 15).ToUniversalTime();

            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            mockIdentityServiceProvider
                .Setup(m => m.Search(IdentitySearchServiceFixture.AtlasClientId, new List<string> {"atlas_user"}))
                .ReturnsAsync(() => new List<UserSearchResponse>
                {
                    new UserSearchResponse
                    {
                        SubjectId = "atlas_user",
                        FirstName = "Robert",
                        MiddleName = "Brian",
                        LastName = "Smith",
                        LastLoginDate = lastLoginDate
                    }
                });

            // search + sort
            var results = _fixture.IdentitySearchService(mockIdentityServiceProvider.Object).Search(
                new IdentitySearchRequest
                {
                    ClientId = IdentitySearchServiceFixture.AtlasClientId,
                    SortKey = "name",
                    SortDirection = "desc"
                }).Result.ToList();

            Assert.Equal(2, results.Count);

            var result1 = results[0];
            Assert.Equal("atlas_user", result1.SubjectId);
            Assert.Equal("Robert", result1.FirstName);
            Assert.Equal("Brian", result1.MiddleName);
            Assert.Equal("Smith", result1.LastName);
            Assert.NotNull(result1.LastLogin);
            Assert.Equal(lastLoginDate, result1.LastLogin.Value.ToUniversalTime());
            Assert.Equal(IdentitySearchServiceFixture.UserAtlasRoleName, result1.Roles.FirstOrDefault());

            var result2 = results[1];
            Assert.Equal(IdentitySearchServiceFixture.AdminAtlasGroupName, result2.Name);
            Assert.Equal(IdentitySearchServiceFixture.AdminAtlasRoleName, result2.Roles.FirstOrDefault());

            // search + sort + paging
            results = _fixture.IdentitySearchService(mockIdentityServiceProvider.Object).Search(
                new IdentitySearchRequest
                {
                    ClientId = IdentitySearchServiceFixture.AtlasClientId,
                    SortKey = "name",
                    SortDirection = "desc",
                    PageSize = 1,
                    PageNumber = 1
                }).Result.ToList();

            Assert.Equal(1, results.Count);

            result1 = results[0];
            Assert.Equal("atlas_user", result1.SubjectId);
            Assert.Equal("Robert", result1.FirstName);
            Assert.Equal("Brian", result1.MiddleName);
            Assert.Equal("Smith", result1.LastName);
            Assert.NotNull(result1.LastLogin);
            Assert.Equal(lastLoginDate, result1.LastLogin.Value.ToUniversalTime());
            Assert.Equal(IdentitySearchServiceFixture.UserAtlasRoleName, result1.Roles.FirstOrDefault());


            // search + sort + filter
            results = _fixture.IdentitySearchService(mockIdentityServiceProvider.Object).Search(
                new IdentitySearchRequest
                {
                    ClientId = IdentitySearchServiceFixture.AtlasClientId,
                    SortKey = "name",
                    SortDirection = "desc",
                    Filter = "brian"
                }).Result.ToList();

            Assert.Equal(1, results.Count);

            result1 = results[0];
            Assert.Equal("atlas_user", result1.SubjectId);
            Assert.Equal("Robert", result1.FirstName);
            Assert.Equal("Brian", result1.MiddleName);
            Assert.Equal("Smith", result1.LastName);
            Assert.NotNull(result1.LastLogin);
            Assert.Equal(lastLoginDate, result1.LastLogin.Value.ToUniversalTime());
            Assert.Equal(IdentitySearchServiceFixture.UserAtlasRoleName, result1.Roles.FirstOrDefault());
        }
    }
}
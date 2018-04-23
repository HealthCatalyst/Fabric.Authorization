using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.API.RemoteServices.Identity.Models;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;
using Serilog;

namespace Fabric.Authorization.UnitTests.Search
{
    public class MemberSearchServiceFixture
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
        public const string DosRoleName = "dosRole";
        public const string DosGroupName = "dosGroup";

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

        private Group _dosGroup;
        private Role _dosRole;

        public MemberSearchServiceFixture()
        {
            InitializeData();

            _mockClientStore.SetupGetClient(new List<Client> {_patientSafetyClient, _atlasClient});

            _mockRoleStore.SetupGetRoles(new List<Role>
            {
                _adminPatientSafetyRole,
                _userPatientSafetyRole,
                _adminAtlasRole,
                _userAtlasRole,
                _dosRole
            });

            _mockPermissionStore.SetupGetPermissions(new List<Permission>());

            _mockGroupStore.SetupGetGroups(new List<Group>
            {
                _adminPatientSafetyGroup,
                _userPatientSafetyGroup,
                _adminAtlasGroup,
                _userAtlasGroup,
                _dosGroup
            });

            var dosSecurableItems = new List<SecurableItem>
            {
                new SecurableItem
                {
                    Id = Guid.NewGuid(),
                    Grain = Domain.Defaults.Authorization.DosGrain,
                    ClientOwner = _atlasClient.Id,
                    Name = "datamarts"
                }
            };

            var securableItems = new List<SecurableItem>
                {
                    _patientSafetyClient.TopLevelSecurableItem,
                    _atlasClient.TopLevelSecurableItem
                }
                .Union(_patientSafetyClient.TopLevelSecurableItem.SecurableItems)
                .Union(_atlasClient.TopLevelSecurableItem.SecurableItems)
                .Union(dosSecurableItems);

            var mockSecurableItemStore = new Mock<ISecurableItemStore>()
                .SetupGetSecurableItem(securableItems.ToList());

            _clientService = new ClientService(_mockClientStore.Create(), mockSecurableItemStore.Create());
            _roleService = new RoleService(_mockRoleStore.Create(), _mockPermissionStore.Create(), _clientService);
        }

        private void InitializeData()
        {
            InitializePatientSafetyData();
            InitializeAtlasData();
        }

        public MemberSearchService MemberSearchService(IIdentityServiceProvider identityServiceProvider)
        {
            var memberSearchService =
                new MemberSearchService(_clientService, _roleService, identityServiceProvider, new Mock<ILogger>().Object);
            return memberSearchService;
        }

        private void InitializePatientSafetyData()
        {
            _adminPatientSafetyRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = AdminPatientSafetyRoleName,
                Grain = "app",
                SecurableItem = "patientsafety",
                Groups = new List<Group>
                {
                    new Group
                    {
                        Name = AdminPatientSafetyGroupName
                    }
                }
            };

            _userPatientSafetyRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = UserPatientSafetyRoleName,
                Grain = "patientsafety",
                SecurableItem = "patient",
                Groups = new List<Group>
                {
                    new Group
                    {
                        Name = UserPatientSafetyGroupName
                    }
                }
            };

            _adminPatientSafetyGroup = new Group
            {
                Id = Guid.NewGuid(),
                Name = AdminPatientSafetyGroupName,
                Roles = new List<Role>
                {
                    new Role
                    {
                        Id = _adminPatientSafetyRole.Id,
                        Name = AdminPatientSafetyRoleName,
                        Grain = "app",
                        SecurableItem = "patientsafety",
                        Groups = new List<Group>
                        {
                            new Group
                            {
                                Name = AdminPatientSafetyGroupName
                            }
                        }
                    }
                },
                Source = "Windows"
            };

            _userPatientSafetyGroup = new Group
            {
                Id = Guid.NewGuid(),
                Name = UserPatientSafetyGroupName,
                Users = new List<User>
                {
                    new User("patientsafety_user", "Windows")
                    {
                        Groups = new List<Group> { new Group { Name = UserPatientSafetyGroupName } }
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
                        Groups = new List<Group>
                        {
                            new Group
                            {
                                Name = UserPatientSafetyGroupName
                            }
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
                SecurableItem = "atlas"
            };

            _adminAtlasGroup = new Group
            {
                Id = Guid.NewGuid(),
                Name = AdminAtlasGroupName,
                Roles = new List<Role>
                {
                    _adminAtlasRole
                }
            };

            _adminAtlasRole.Groups = new List<Group> {_adminAtlasGroup};

            _userAtlasRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = UserAtlasRoleName,
                Grain = "atlas",
                SecurableItem = "atlas-si2",
                Users = new List<User>
                {
                    new User("atlas_user", "Windows")
                    {
                        SubjectId = "atlas_user",
                        IdentityProvider = "Windows"
                    }
                }
            };

            _userAtlasGroup = new Group
            {
                Id = Guid.NewGuid(),
                Name = UserAtlasGroupName,
                Roles = new List<Role>
                {
                    _userAtlasRole
                },
                Source = "Custom"
            };

            _userAtlasRole.Groups = new List<Group> {_userAtlasGroup};
            _userAtlasRole.Users.First().Groups = new List<Group> {_userAtlasGroup};

            _dosRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = DosRoleName,
                Grain = Domain.Defaults.Authorization.DosGrain,
                SecurableItem = "datamarts"
            };

            _dosGroup = new Group
            {
                Id = Guid.NewGuid(),
                Name = DosGroupName,
                Roles = new List<Role>
                {
                    _dosRole
                }
            };

            _dosRole.Groups = new List<Group>
            {
                _dosGroup
            };
        }
    }

    [Collection("Identity Search Tests")]
    public class MemberSearchServiceTests : IClassFixture<MemberSearchServiceFixture>
    {
        public MemberSearchServiceTests(MemberSearchServiceFixture fixture)
        {
            _fixture = fixture;
        }

        private readonly MemberSearchServiceFixture _fixture;

        [Fact]
        public async Task MemberSearch_ClientIdDoesNotExist_NotFoundExceptionAsync()
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            var identitySearchService = _fixture.MemberSearchService(mockIdentityServiceProvider.Object);

            await Assert.ThrowsAsync<NotFoundException<Client>>(
                () => identitySearchService.Search(new MemberSearchRequest
                {
                    ClientId = "xyz"
                }));
        }

        [Fact]
        public void MemberSearch_ValidRequest_Success()
        {
            var lastLoginDate = new DateTime(2017, 9, 15).ToUniversalTime();

            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            mockIdentityServiceProvider
                .Setup(m => m.Search(MemberSearchServiceFixture.AtlasClientId, new List<string> {"atlas_user:Windows"}))
                .ReturnsAsync(() => new FabricIdentityUserResponse
                {
                    HttpStatusCode = HttpStatusCode.OK,
                    Results = new List<UserSearchResponse>
                    {
                        new UserSearchResponse
                        {
                            SubjectId = "atlas_user",
                            FirstName = "Robert",
                            MiddleName = "Brian",
                            LastName = "Smith",
                            LastLoginDate = lastLoginDate
                        }
                    }
                });

            // search + sort
            var results = _fixture.MemberSearchService(mockIdentityServiceProvider.Object).Search(
                new MemberSearchRequest
                {
                    ClientId = MemberSearchServiceFixture.AtlasClientId,
                    SortKey = "name",
                    SortDirection = "desc"
                }).Result.Results.ToList();

            Assert.Equal(4, results.Count);

            var result0 = results[0];
            Assert.Equal(MemberSearchServiceFixture.UserAtlasGroupName, result0.Name);
            Assert.Equal(MemberSearchServiceFixture.UserAtlasRoleName, result0.Roles.FirstOrDefault()?.Name);

            var result1 = results[1];
            Assert.Equal("atlas_user", result1.SubjectId);
            Assert.Equal("Robert", result1.FirstName);
            Assert.Equal("Brian", result1.MiddleName);
            Assert.Equal("Smith", result1.LastName);
            Assert.NotNull(result1.LastLoginDateTimeUtc);
            Assert.Equal(lastLoginDate, result1.LastLoginDateTimeUtc.Value.ToUniversalTime());
            Assert.Empty(result1.Roles);

            var result2 = results[2];
            Assert.Equal(MemberSearchServiceFixture.DosGroupName, result2.Name);
            Assert.Equal(MemberSearchServiceFixture.DosRoleName, result2.Roles.FirstOrDefault()?.Name);

            var result3 = results[3];
            Assert.Equal(MemberSearchServiceFixture.AdminAtlasGroupName, result3.Name);
            Assert.Equal(MemberSearchServiceFixture.AdminAtlasRoleName, result3.Roles.FirstOrDefault()?.Name);

            // search + sort + paging
            results = _fixture.MemberSearchService(mockIdentityServiceProvider.Object).Search(
                new MemberSearchRequest
                {
                    ClientId = MemberSearchServiceFixture.AtlasClientId,
                    SortKey = "name",
                    SortDirection = "desc",
                    PageSize = 1,
                    PageNumber = 1
                }).Result.Results.ToList();

            Assert.Single(results);

            result0 = results[0];
            Assert.Equal(MemberSearchServiceFixture.UserAtlasGroupName, result0.Name);
            Assert.Equal(MemberSearchServiceFixture.UserAtlasRoleName, result0.Roles.FirstOrDefault()?.Name);

            // search + sort + filter
            results = _fixture.MemberSearchService(mockIdentityServiceProvider.Object).Search(
                new MemberSearchRequest
                {
                    ClientId = MemberSearchServiceFixture.AtlasClientId,
                    SortKey = "name",
                    SortDirection = "desc",
                    Filter = "brian"
                }).Result.Results.ToList();

            Assert.Single(results);

            result0 = results[0];
            Assert.Equal("atlas_user", result0.SubjectId);
            Assert.Equal("Robert", result0.FirstName);
            Assert.Equal("Brian", result0.MiddleName);
            Assert.Equal("Smith", result0.LastName);
            Assert.NotNull(result0.LastLoginDateTimeUtc);
            Assert.Equal(lastLoginDate, result0.LastLoginDateTimeUtc.Value.ToUniversalTime());
            Assert.Empty(result0.Roles);
        }
    }
}
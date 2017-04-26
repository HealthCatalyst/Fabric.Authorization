using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain
{
    public class InMemoryUserStore : IUserStore
    {
        private static readonly ConcurrentDictionary<string, User> Users = new ConcurrentDictionary<string, User>();

        static InMemoryUserStore()
        {
            var user1 = new User
            {
                Id = "mvidal",
                Roles = new List<Role>
                {
                    new Role
                    {
                        Id = 1,
                        Grain = "app",
                        Resource = "patientsafety",
                        Name = "Admin",
                        Permissions = new List<Permission>
                        {
                            new Permission
                            {
                                Id = 1,
                                Grain = "app",
                                Resource = "patientsafety",
                                Name = "manageusers"
                            },
                            new Permission
                            {
                                Id = 2,
                                Grain = "app",
                                Resource = "patientsafety",
                                Name = "createalerts"
                            }
                        }
                    },
                    new Role
                    {
                        Id = 2,
                        Grain = "app",
                        Resource = "sourcemartdesigner",
                        Name = "Admin",
                        Permissions = new List<Permission>
                        {
                            new Permission
                            {
                                Id = 3,
                                Grain = "app",
                                Resource = "sourcemartdesigner",
                                Name = "createalerts"
                            }
                        }
                    }
                }
            };
            Users.TryAdd(user1.Id, user1);
        }
        
        public User GetUser(string userId)
        {
            return Users.ContainsKey(userId) ? Users[userId] : null;
        }

        public void AddUser(User user)
        {
            Users.TryAdd(user.Id, user);
        }

        public void UpdateUser(User user)
        {
            //do nothing since this is an in memory store
        }
    }
}

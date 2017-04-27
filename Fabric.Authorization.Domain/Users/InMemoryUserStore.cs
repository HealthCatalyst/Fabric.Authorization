using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Users
{
    public class InMemoryUserStore : IUserStore
    {
        private static readonly ConcurrentDictionary<string, User> Users = new ConcurrentDictionary<string, User>();

        static InMemoryUserStore()
        {
            var user1 = new User
            {
                Id = "mvidal"
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class UserService
    {
        private readonly IUserStore _userStore;

        public UserService(IUserStore userStore)
        {
            _userStore = userStore;
        }

        public async Task<IEnumerable<string>> GetGroupsForUser(string subjectId, string identityProvider)
        {
            var user = await _userStore.Get($"{subjectId}:{identityProvider}");
            return user != null ? user.Groups : new List<string>();
        }

        public async Task<User> AddUser(User user)
        {
            return await _userStore.Add(user);
        }

        public async Task<bool> Exists(string subjectId, string identityProvider)
        {
            return await _userStore.Exists($"{subjectId}:{identityProvider}");
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
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
            return user.Groups;
        }
    }
}
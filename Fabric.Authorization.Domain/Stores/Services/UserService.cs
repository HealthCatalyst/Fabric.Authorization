using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fabric.Authorization.Domain.Stores.Services
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
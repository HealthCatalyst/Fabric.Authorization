using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.Services
{
    public class UserService
    {
        private readonly IUserStore _userStore;

        public UserService(IUserStore userStore)
        {
            _userStore = userStore;
        }

        public async Task<User> GetUser(string subjectId)
        {
            return await _userStore.Get(subjectId);
        }

        public async Task<IEnumerable<string>> GetGroupsForUser(string subjectId)
        {
            var user = await _userStore.Get(subjectId);
            return user.Groups;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.InMemory
{
    public class InMemoryUserStore : InMemoryGenericStore<User>, IUserStore
    {
        public override async Task<User> Add(User model)
        {
            model.Id = model.SubjectId;
            return await base.Add(model);
        }

        public Task<IEnumerable<User>> GetUsers(string subjectId = null)
        {
            var users = Dictionary.Select(kvp => kvp.Value);

            if (!string.IsNullOrEmpty(subjectId))
            {
                users = users.Where(r => r.SubjectId == subjectId);
            }

            return Task.FromResult(users.Where(u => !u.IsDeleted));
        }
    }
}
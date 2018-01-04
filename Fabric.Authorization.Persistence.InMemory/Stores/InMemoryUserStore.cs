using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Persistence.InMemory.Stores
{
    public class InMemoryUserStore : InMemoryFormattableIdentifierStore<User>, IUserStore
    {
        public InMemoryUserStore(IIdentifierFormatter identifierFormatter) : base(identifierFormatter)
        {
            
        }

        public override async Task<User> Add(User model)
        {
            model.Id = FormatId(model.Identifier);
            return await base.Add(model);
        }

        public Task<IEnumerable<User>> GetUsers(string subjectId = null, string identityProvider = null)
        {
            var users = Dictionary.Select(kvp => kvp.Value);

            if (!string.IsNullOrEmpty(subjectId))
            {
                users = users.Where(u => string.Equals(u.SubjectId, subjectId, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(identityProvider))
            {
                users = users.Where(u => string.Equals(u.IdentityProvider, identityProvider, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(users.Where(u => !u.IsDeleted));
        }
    }
}
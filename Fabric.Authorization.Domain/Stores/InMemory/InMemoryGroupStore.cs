using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.InMemory
{
    public class InMemoryGroupStore : InMemoryFormattableIdentifierStore<Group>, IGroupStore
    {
        [Obsolete]
        public InMemoryGroupStore(IIdentifierFormatter identifierFormatter) : base(identifierFormatter)
        {
            var group1 = new Group
            {
                Id = Guid.NewGuid().ToString(),
                Name = @"FABRIC\Health Catalyst Viewer",
            };

            var group2 = new Group
            {
                Id = Guid.NewGuid().ToString(),
                Name = @"FABRIC\Health Catalyst Editor",
            };

            this.Add(group1).Wait();
            this.Add(group2).Wait();
        }

        public override async Task<Group> Add(Group group)
        {
            group.Track();

            var formattedId = FormatId(group.Identifier);

            if (Dictionary.ContainsKey(formattedId))
            {
                var existingGroup = await Get(formattedId);
                if (existingGroup == null)
                {
                    throw new CouldNotCompleteOperationException();
                }
                if (!existingGroup.IsDeleted)
                {
                    throw new AlreadyExistsException<Group>(group, formattedId);
                }

                existingGroup.IsDeleted = false;
                await Update(existingGroup).ConfigureAwait(false);
                return existingGroup;
            }

            Dictionary.TryAdd(formattedId, group);
            return group;
        }
    }
}
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
                var existingGroup = Dictionary[formattedId];
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

        public override async Task Delete(Group group)
        {
            group.IsDeleted = true;

            var formattedId = FormatId(group.Id);

            // use base class Exists so IsDeleted is ignored since it's already been updated at this point
            if (await base.Exists(formattedId).ConfigureAwait(false))
            {
                if (!Dictionary.TryUpdate(formattedId, group, Dictionary[formattedId]))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new NotFoundException<Group>(group, group.Identifier);
            }
        }

        public override async Task<bool> Exists(string id)
        {
            var formattedId = FormatId(id);
            return await base.Exists(formattedId) && !Dictionary[formattedId].IsDeleted;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Serilog;

namespace Fabric.Authorization.Domain.Services
{
    public class GroupMigratorService
    {
        private readonly IGroupStore _groupStore;
        private readonly ILogger _logger;

        public GroupMigratorService(
            IGroupStore groupStore,
            ILogger logger)
        {
            _groupStore = groupStore ?? throw new ArgumentNullException(nameof(groupStore));
            _logger = logger;
        }

        public async Task<GroupMigrationResult> MigrateDuplicateGroups()
        {
            var groups = (await _groupStore.GetAll()).ToList();

            var groupKeys = groups
                .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            var groupMigrationResult = new GroupMigrationResult();
            foreach (var key in groupKeys)
            {
                var duplicateGroups = groups.Where(g => string.Equals(key, g.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                var originalGroup = duplicateGroups.First();
                duplicateGroups.RemoveAt(0);

                var groupMigrationRecord = new GroupMigrationRecord
                {
                    DuplicateCount = duplicateGroups.Count,
                    GroupName = originalGroup.Name
                };

                foreach (var duplicateGroup in duplicateGroups)
                {
                    // migrate roles
                    foreach (var role in duplicateGroup.Roles)
                    {
                        var roleExists = originalGroup.Roles.Any(r => r.Equals(role));
                        if (!roleExists)
                        {
                            _logger.Information($"Migrating Role {role} from {duplicateGroup} to group {originalGroup}");
                            originalGroup.Roles.Add(role);
                        }
                    }

                    // migrate users
                    foreach (var user in duplicateGroup.Users)
                    {
                        var userExists = originalGroup.Users.Any(u => new UserComparer().Equals(u, user));
                        if (!userExists)
                        {
                            _logger.Information($"Migrating User {user} from {duplicateGroup} to group {originalGroup}");
                            originalGroup.Users.Add(user);
                        }
                    }

                    // migrate directory groups to new parents (this will not execute if the duplicate group is a custom group)
                    foreach (var parent in duplicateGroup.Parents)
                    {
                        var parentExists = originalGroup.Parents.Any(p => p.Id == parent.Id);
                        if (!parentExists)
                        {
                            _logger.Information($"Migrating Group {originalGroup} to Parent Group {parent}");
                            originalGroup.Parents.Add(parent);
                            /*
                            try
                            {
                                await _groupStore.AddChildGroups(parent, new List<Group> {originalGroup});
                            }
                            catch (Exception e)
                            {
                                var msg = $"Error migrating Directory Child Group {originalGroup} to Parent Group {parent}";
                                _logger.Error(e, msg);
                                groupMigrationRecord.Errors.Add($"{msg} ({e.Message})");
                            }*/
                        }
                    }

                    // migrate custom parent groups to new child groups (this will not execute if the duplicate group is a directory group)
                    foreach (var child in duplicateGroup.Children)
                    {
                        var childExists = originalGroup.Children.Any(c => c.Id == child.Id);
                        if (!childExists)
                        {
                            _logger.Information($"Error migrating Custom Parent Group {originalGroup} to Child Group {child}");
                            originalGroup.Children.Add(child);
                            /*
                            try
                            {
                                await _groupStore.AddChildGroups(originalGroup, new List<Group> {child});
                            }
                            catch (Exception e)
                            {
                                var msg = $"Error migrating Custom Parent Group {originalGroup} to Child Group {child}";
                                _logger.Error(e, msg);
                                groupMigrationRecord.Errors.Add($"{msg} ({e.Message})");
                            }*/
                        }
                    }

                    try
                    {
                        await _groupStore.Update(originalGroup);
                    }
                    catch (Exception e)
                    {
                        var msg = $"Exception thrown while updating database with migration from group {duplicateGroup} to {originalGroup}.";
                        _logger.Error(e, msg);
                        groupMigrationRecord.Errors.Add($"{msg} ({e.Message})");
                    }

                    try
                    {
                        await _groupStore.Delete(duplicateGroup);
                    }
                    catch (Exception e)
                    {
                        var msg = $"Exception thrown while deleting DUPLICATE group {duplicateGroup}";
                        _logger.Error(e, msg);
                        groupMigrationRecord.Errors.Add($"{msg} ({e.Message})");
                    }
                }

                groupMigrationResult.GroupMigrationRecords.Add(groupMigrationRecord);
            }

            return groupMigrationResult;
        }
    }

    public class GroupMigrationResult
    {
        public IList<GroupMigrationRecord> GroupMigrationRecords { get; set; } = new List<GroupMigrationRecord>();
    }

    public class GroupMigrationRecord
    {
        public string GroupName { get; set; }
        public int DuplicateCount { get; set; }
        public IList<string> Errors { get; set; } = new List<string>();
        public int ErrorCount => Errors.Count;
    }
}

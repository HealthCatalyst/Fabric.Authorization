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
                .GroupBy(g => g.GroupIdentifier, new GroupIdentifierComparer())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            var groupMigrationResult = new GroupMigrationResult();
            foreach (var key in groupKeys)
            {
                var duplicateGroups = groups.Where(g => new GroupIdentifierComparer().Equals(g.GroupIdentifier, key)).ToList();
                var originalGroup = duplicateGroups.First();
                duplicateGroups.RemoveAt(0);

                var groupMigrationRecord = new GroupMigrationRecord
                {
                    DuplicateCount = duplicateGroups.Count
                };

                var deletedDuplicateGroups = new List<Group>();
                foreach (var duplicateGroup in duplicateGroups)
                {
                    try
                    {
                        await _groupStore.Delete(duplicateGroup);
                        deletedDuplicateGroups.Add(duplicateGroup);
                    }
                    catch (Exception e)
                    {
                        var msg = $"Exception thrown while deleting DUPLICATE group {duplicateGroup}";
                        _logger.Error(e, msg);
                        groupMigrationRecord.Errors.Add($"{msg} ({e.Message})");
                    }
                }

                // only migrate the groups that were successfully deleted
                foreach (var duplicateGroup in deletedDuplicateGroups)
                {
                    // migrate roles
                    foreach (var role in duplicateGroup.Roles)
                    {
                        var roleExists = originalGroup.Roles.Any(r => r.Equals(role));
                        if (!roleExists)
                        {
                            _logger.Information($"Migrating Role {role} from {duplicateGroup} to group {originalGroup}");
                            try
                            {
                                await _groupStore.AddRolesToGroup(originalGroup, new List<Role> {role});
                            }
                            catch (Exception e)
                            {
                                var msg = $"Error migrating Role {role} to Group {originalGroup}";
                                _logger.Error(e, msg);
                                groupMigrationRecord.Errors.Add($"{msg} ({e.Message})");
                            }
                        }
                    }

                    // migrate users (applies custom groups only)
                    foreach (var user in duplicateGroup.Users)
                    {
                        var userExists = originalGroup.Users.Any(u => new UserComparer().Equals(u, user));
                        if (!userExists)
                        {
                            _logger.Information($"Migrating User {user} from {duplicateGroup} to group {originalGroup}");
                            try
                            {
                                await _groupStore.AddUsersToGroup(originalGroup, new List<User> {user});
                            }
                            catch (Exception e)
                            {
                                var msg = $"Error migrating User {user} to Group {originalGroup}";
                                _logger.Error(e, msg);
                                groupMigrationRecord.Errors.Add($"{msg} ({e.Message})");
                            }
                        }
                    }

                    // migrate directory groups to new parents (this will not execute if the duplicate group is a custom group)
                    foreach (var parent in duplicateGroup.Parents)
                    {
                        var parentExists = originalGroup.Parents.Any(p => p.Id == parent.Id);
                        if (!parentExists)
                        {
                            _logger.Information($"Migrating Group {originalGroup} to Parent Group {parent}");
                            try
                            {
                                await _groupStore.AddChildGroups(parent, new List<Group> {originalGroup});
                            }
                            catch (Exception e)
                            {
                                var msg = $"Error migrating Directory Child Group {originalGroup} to Parent Group {parent}";
                                _logger.Error(e, msg);
                                groupMigrationRecord.Errors.Add($"{msg} ({e.Message})");
                            }
                        }
                    }

                    // migrate custom parent groups to new child groups (this will not execute if the duplicate group is a directory group)
                    foreach (var child in duplicateGroup.Children)
                    {
                        var childExists = originalGroup.Children.Any(c => c.Id == child.Id);
                        if (!childExists)
                        {
                            _logger.Information($"Error migrating Custom Parent Group {originalGroup} to Child Group {child}");
                            try
                            {
                                await _groupStore.AddChildGroups(originalGroup, new List<Group> {child});
                            }
                            catch (Exception e)
                            {
                                var msg = $"Error migrating Custom Parent Group {originalGroup} to Child Group {child}";
                                _logger.Error(e, msg);
                                groupMigrationRecord.Errors.Add($"{msg} ({e.Message})");
                            }
                        }
                    }
                }

                groupMigrationRecord.MigratedGroup = originalGroup;
                groupMigrationResult.GroupMigrationRecords.Add(groupMigrationRecord);
            }

            return groupMigrationResult;
        }

        public async Task MigrateWindowsSourceToDirectory()
        {
            var groups = (await _groupStore.GetAll()).Where(g => string.Equals(g.Source, "windows", StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var group in groups)
            {
                group.Source = GroupConstants.DirectorySource;
                await _groupStore.Update(group);
            }
        }

        public async Task MigrateIdentityProvider()
        {
            var groups = (await _groupStore.GetAll()).Where(g => string.Equals(g.Source, GroupConstants.DirectorySource, StringComparison.OrdinalIgnoreCase) && g.IdentityProvider == null).ToList();

            foreach (var group in groups)
            {
                group.IdentityProvider = IdentityConstants.ActiveDirectory;
                await _groupStore.Update(group);
            }
        }
    }

    public class GroupMigrationResult
    {
        public IList<GroupMigrationRecord> GroupMigrationRecords { get; set; } = new List<GroupMigrationRecord>();
    }

    public class GroupMigrationRecord
    {
        public Group MigratedGroup { get; set; }
        public int DuplicateCount { get; set; }
        public IList<string> Errors { get; set; } = new List<string>();
    }
}

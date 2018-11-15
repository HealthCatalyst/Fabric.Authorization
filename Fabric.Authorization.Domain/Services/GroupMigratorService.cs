using System;
using System.Collections.Generic;
using System.Linq;
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

        public async void MigrateDuplicateGroups()
        {
            var groups = (await _groupStore.GetAll()).ToList();

            var groupKeys = groups
                .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var key in groupKeys)
            {
                var duplicateGroups = groups.Where(g => string.Equals(key, g.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                var originalGroup = duplicateGroups.First();
                duplicateGroups.RemoveAt(0);

                foreach (var duplicateGroup in duplicateGroups)
                {
                    // migrate roles
                    foreach (var role in duplicateGroup.Roles)
                    {
                        var roleExists = originalGroup.Roles.Any(r => r.Equals(role));
                        if (!roleExists)
                        {
                            _logger.Information($"Migrating Role {role} to group {originalGroup}");
                            originalGroup.Roles.Add(role);
                        }
                    }

                    // migrate users
                    foreach (var user in duplicateGroup.Users)
                    {
                        var userExists = originalGroup.Users.Any(u => new UserComparer().Equals(u, user));
                        if (!userExists)
                        {
                            _logger.Information($"Migrating User {user} to group {originalGroup}");
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
                            try
                            {
                                await _groupStore.AddChildGroups(parent, new List<Group> {originalGroup});
                            }
                            catch (Exception e)
                            {
                                _logger.Error(e, $"Migrating Child Group {originalGroup} to Parent Group {parent}");
                            }
                        }
                    }

                    // migrate custom parent groups to new child groups (this will not execute if the duplicate group is a directory group)
                    foreach (var child in duplicateGroup.Children)
                    {
                        var childExists = originalGroup.Children.Any(c => c.Id == child.Id);
                        if (!childExists)
                        {
                            _logger.Information($"Migrating Group {originalGroup} to Child Group {child}");
                            try
                            {
                                await _groupStore.AddChildGroups(originalGroup, new List<Group> {child});
                            }
                            catch (Exception e)
                            {
                                _logger.Error(e, $"Migrating Parent Group {originalGroup} to Child Group {child}");
                            }
                        }
                    }

                    try
                    {
                        await _groupStore.Update(originalGroup);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Exception thrown while migrating ORIGINAL group {originalGroup}");
                    }

                    try
                    {
                        duplicateGroup.IsDeleted = true;
                        await _groupStore.Update(duplicateGroup);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Exception thrown while deprecating DUPLICATE group {duplicateGroup}");
                    }
                }
            }
        }
    }
}

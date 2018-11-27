using System;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using FluentValidation.Results;
using System.Collections.Generic;
using Catalyst.Fabric.Authorization.Models;
using Catalyst.Fabric.Authorization.Models.Requests;
using Fabric.Authorization.Domain.Resolvers.Models;

namespace Fabric.Authorization.API.Models
{
    public static class ModelExtensions
    {
        public static RoleApiModel ToRoleApiModel(this Role role)
        {
            var roleApiModel = new RoleApiModel
            {
                Id = role.Id,
                Grain = role.Grain,
                SecurableItem = role.SecurableItem,
                Name = role.Name,
                DisplayName = role.DisplayName,
                Description = role.Description,
                ParentRole = role.ParentRole,
                ChildRoles = role.ChildRoles.ToList(),
                Permissions = role.Permissions?.Select(p => p.ToPermissionApiModel()),
                DeniedPermissions = role.DeniedPermissions?.Select(p => p.ToPermissionApiModel()),
                CreatedDateTimeUtc = role.CreatedDateTimeUtc,
                ModifiedDateTimeUtc = role.ModifiedDateTimeUtc,
                CreatedBy = role.CreatedBy,
                ModifiedBy = role.ModifiedBy
            };
            return roleApiModel;
        }

        public static IEnumerable<RoleApiModel> ToRoleApiModels(this IEnumerable<Role> roles, string grain, string securableItem, Func<Role, string, string, bool> groupRoleFilter)
        {
            return roles.Where(r => !r.IsDeleted
                                    && groupRoleFilter(r, grain, securableItem))
                .Select(r => r.ToRoleApiModel());
        }

        public static Role ToRoleDomainModel(this RoleApiModel role)
        {
            var roleDomainModel = new Role
            {
                Id = role.Id ?? Guid.Empty,
                Grain = role.Grain,
                SecurableItem = role.SecurableItem,
                Name = role.Name,
                DisplayName = role.DisplayName,
                Description = role.Description,
                ParentRole = role.ParentRole,
                ChildRoles = role.ChildRoles?.ToList() ?? new List<Guid>(),
                Permissions = role.Permissions?.Select(p => p.ToPermissionDomainModel()).ToList() ?? new List<Permission>(),
                DeniedPermissions = role.DeniedPermissions?.Select(p => p.ToPermissionDomainModel()).ToList() ?? new List<Permission>(),
                CreatedDateTimeUtc = role.CreatedDateTimeUtc,
                ModifiedDateTimeUtc = role.ModifiedDateTimeUtc,
                CreatedBy = role.CreatedBy,
                ModifiedBy = role.ModifiedBy
            };
            return roleDomainModel;
        }

        public static User ToUserDomainModel(this UserApiModel user)
        {
            var userDomainModel = new User(user.SubjectId, user.IdentityProvider);
            return userDomainModel;
        }

        public static UserApiModel ToUserApiModel(this User user)
        {
            var userApiModel = new UserApiModel
            {
                SubjectId = user.SubjectId,
                IdentityProvider = user.IdentityProvider,
                Groups = user.Groups.Select(g => g.Name).ToList(),
                Roles = user.Roles?.Where(r => !r.IsDeleted).Select(r => r.ToRoleApiModel()).ToList()
            };

            return userApiModel;
        }

        public static GroupRoleApiModel ToGroupRoleApiModel(this Group group, bool isRequestedGroup = true)
        {
            var groupRoleApiModel = new GroupRoleApiModel
            {
                Id = group.Id,
                GroupName = group.Name,
                IdentityProvider = group.IdentityProvider,
                DisplayName = group.DisplayName,
                Description = group.Description,
                Roles = group.Roles?.Where(r => !r.IsDeleted).Select(r => r.ToRoleApiModel()),
                GroupSource = group.Source,
                TenantId = group.TenantId,
                Parents = isRequestedGroup ? group.Parents.Select(p => p.ToGroupRoleApiModel(false)) : new List<GroupRoleApiModel>(),
                Children = isRequestedGroup ? group.Children.Select(c => c.ToGroupRoleApiModel(false)) : new List<GroupRoleApiModel>()
            };

            return groupRoleApiModel;
        }

        public static GroupUserApiModel ToGroupUserApiModel(this Group group)
        {
            var groupUserApiModel = new GroupUserApiModel
            {
                Id = group.Id,
                GroupName = group.Name,
                DisplayName = group.DisplayName,
                Description = group.Description,
                Users = group.Users?.Where(u => !u.IsDeleted).Select(r => r.ToUserApiModel()),
                GroupSource = group.Source
            };

            return groupUserApiModel;
        }

        public static Group ToGroupDomainModel(this GroupRoleApiModel groupRoleApiModel)
        {
            var group = new Group
            {
                Id = groupRoleApiModel.Id ?? new Guid(),
                Name = groupRoleApiModel.GroupName,
                IdentityProvider = groupRoleApiModel.IdentityProvider,
                DisplayName = groupRoleApiModel.DisplayName,
                Description = groupRoleApiModel.Description,
                Source = groupRoleApiModel.GroupSource,
                TenantId = groupRoleApiModel.TenantId
            };

            return group;
        }

        public static Group ToGroupDomainModel(this GroupPostApiRequest groupPostApiRequest)
        {
            var group = new Group
            {
                Name = groupPostApiRequest.GroupName,
                IdentityProvider = groupPostApiRequest.IdentityProvider,
                DisplayName = groupPostApiRequest.DisplayName,
                Description = groupPostApiRequest.Description,
                Source = groupPostApiRequest.GroupSource,
                TenantId = groupPostApiRequest.TenantId,
                ExternalIdentifier = groupPostApiRequest.ExternalIdentifier
            };

            return group;
        }

        public static PermissionApiModel ToPermissionApiModel(this Permission permission)
        {
            var permissionApiModel = new PermissionApiModel
            {
                Id = permission.Id,
                Grain = permission.Grain,
                Name = permission.Name,
                SecurableItem = permission.SecurableItem,
                CreatedDateTimeUtc = permission.CreatedDateTimeUtc,
                ModifiedDateTimeUtc = permission.ModifiedDateTimeUtc,
                CreatedBy = permission.CreatedBy,
                ModifiedBy = permission.ModifiedBy
            };
            return permissionApiModel;
        }

        public static ResolvedPermissionApiModel ToResolvedPermissionApiModel(this ResolvedPermission resolvedPermission)
        {
            return new ResolvedPermissionApiModel
            {
                Id = resolvedPermission.Id,
                Grain = resolvedPermission.Grain,
                Name = resolvedPermission.Name,
                SecurableItem = resolvedPermission.SecurableItem,
                PermissionAction = (PermissionAction) Enum.Parse(typeof(PermissionAction), resolvedPermission.Action, true),
                Roles = resolvedPermission.Roles.Select(r => r.ToPermissionRoleApiModel()),
                CreatedDateTimeUtc = resolvedPermission.CreatedDateTimeUtc,
                ModifiedDateTimeUtc = resolvedPermission.ModifiedDateTimeUtc,
                CreatedBy = resolvedPermission.CreatedBy,
                ModifiedBy = resolvedPermission.ModifiedBy
            };
        }

        public static PermissionRoleApiModel ToPermissionRoleApiModel(this ResolvedPermissionRole role)
        {
            var permissionRoleApiModel = new PermissionRoleApiModel
            {
                Id = role.Id,
                Name = role.Name
            };

            return permissionRoleApiModel;
        }

        public static Permission ToPermissionDomainModel(this PermissionApiModel permissionApiModel)
        {
            var permission = new Permission
            {
                Id = permissionApiModel.Id ?? Guid.Empty,
                Grain = permissionApiModel.Grain,
                Name = permissionApiModel.Name,
                SecurableItem = permissionApiModel.SecurableItem,
                CreatedDateTimeUtc = permissionApiModel.CreatedDateTimeUtc,
                ModifiedDateTimeUtc = permissionApiModel.ModifiedDateTimeUtc,
                CreatedBy = permissionApiModel.CreatedBy,
                ModifiedBy = permissionApiModel.ModifiedBy
            };
            return permission;
        }

        public static ClientApiModel ToClientApiModel(this Client client)
        {
            var clientApiModel = new ClientApiModel
            {
                Id = client.Id,
                Name = client.Name,
                CreatedDateTimeUtc = client.CreatedDateTimeUtc,
                ModifiedDateTimeUtc = client.ModifiedDateTimeUtc,
                CreatedBy = client.CreatedBy,
                ModifiedBy = client.ModifiedBy,
                TopLevelSecurableItem = client.TopLevelSecurableItem?.ToSecurableItemApiModel()

            };
            return clientApiModel;
        }        

        public static Client ToClientDomainModel(this ClientApiModel client)
        {
            var clientModel = new Client
            {
                Id = client.Id,
                Name = client.Name,
                CreatedDateTimeUtc = client.CreatedDateTimeUtc,
                ModifiedDateTimeUtc = client.ModifiedDateTimeUtc,
                CreatedBy = client.CreatedBy,
                ModifiedBy = client.ModifiedBy,
                TopLevelSecurableItem = client.TopLevelSecurableItem?.ToSecurableItemDomainModel()

            };
            return clientModel;
        }

        public static IEnumerable<GrainApiModel> ToGrainApiModels(this IEnumerable<Grain> grains)
        {
            return grains.Select(g => g.ToGrainApiModel());
        }

        public static GrainApiModel ToGrainApiModel(this Grain grain)
        {
            var grainApiModel = new GrainApiModel
            {
                Id = grain.Id,
                Name = grain.Name,
                SecurableItems = grain.SecurableItems?.Select(s => s.ToSecurableItemApiModel()).ToList(),
                CreatedDateTimeUtc = grain.CreatedDateTimeUtc,
                CreatedBy = grain.CreatedBy,
                ModifiedDateTimeUtc = grain.ModifiedDateTimeUtc,
                ModifiedBy = grain.ModifiedBy,
                RequiredWriteScopes = grain.RequiredWriteScopes,
                IsShared = grain.IsShared
            };

            return grainApiModel;
        }

        public static SecurableItemApiModel ToSecurableItemApiModel(this SecurableItem securableItem)
        {
            var securableItemApiModel = new SecurableItemApiModel
            {
                Id = securableItem.Id,
                Name = securableItem.Name,
                Grain = securableItem.Grain,
                SecurableItems = securableItem.SecurableItems?.Select(s => s.ToSecurableItemApiModel()).ToList(),
                CreatedDateTimeUtc = securableItem.CreatedDateTimeUtc,
                CreatedBy = securableItem.CreatedBy,
                ModifiedDateTimeUtc = securableItem.ModifiedDateTimeUtc,
                ModifiedBy = securableItem.ModifiedBy,
                ClientOwner = securableItem.ClientOwner
            };
            return securableItemApiModel;
        }

        public static SecurableItem ToSecurableItemDomainModel(this SecurableItemApiModel securableItem)
        {
            var securableItemModel = new SecurableItem
            {
                Id = securableItem.Id ?? Guid.Empty,
                Name = securableItem.Name,
                Grain = securableItem.Grain,
                SecurableItems = securableItem.SecurableItems?.Select(s => s.ToSecurableItemDomainModel()).ToList(),
                CreatedDateTimeUtc = securableItem.CreatedDateTimeUtc,
                CreatedBy = securableItem.CreatedBy,
                ModifiedDateTimeUtc = securableItem.ModifiedDateTimeUtc,
                ModifiedBy = securableItem.ModifiedBy
            };
            return securableItemModel;
        }

        public static GroupIdentifier ToGroupIdentifierDomainModel(this GroupIdentifierApiRequest groupIdentifierApiRequest)
        {
            return new GroupIdentifier
            {
                GroupName = groupIdentifierApiRequest.GroupName,
                TenantId = groupIdentifierApiRequest.TenantId,
                IdentityProvider = groupIdentifierApiRequest.IdentityProvider
            };
        }

        public static Error ToError(this ValidationResult validationResult)
        {
            var details = validationResult.Errors.Select(validationResultError => new Error
            {
                Code = validationResultError.ErrorCode,
                Message = validationResultError.ErrorMessage,
                Target = validationResultError.PropertyName
            })
            .ToList();

            var error = new Error
            {
                Message = details.Count > 1 ? "Multiple Errors" : details.FirstOrDefault()?.Message,
                Details = details.ToArray()
            };

            return error;
        }
    }
}

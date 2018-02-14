using System;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using FluentValidation.Results;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Resolvers.Models;
using HttpStatusCode = Nancy.HttpStatusCode;

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

        public static Role ToRoleDomainModel(this RoleApiModel role)
        {
            var roleDomainModel = new Role
            {
                Id = role.Id ?? Guid.Empty,
                Grain = role.Grain,
                SecurableItem = role.SecurableItem,
                Name = role.Name,
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
                Groups = user.Groups
            };

            return userApiModel;
        }

        public static GroupRoleApiModel ToGroupRoleApiModel(this Group group)
        {
            var groupRoleApiModel = new GroupRoleApiModel
            {
                Id = group.Id,
                GroupName = group.Name,
                Roles = group.Roles?.Where(r => !r.IsDeleted).Select(r => r.ToRoleApiModel()),
                GroupSource = group.Source
            };

            return groupRoleApiModel;
        }

        public static GroupRoleApiModel ToGroupRoleApiModel(this Group group, GroupRoleRequest groupRoleRequest, Func<Role, string, string, bool> groupRoleFilter)
        {
            var groupRoleApiModel = new GroupRoleApiModel
            {
                Id = group.Id,
                GroupName = group.Name,
                Roles = group.Roles?
                    .Where(r => !r.IsDeleted 
                        && groupRoleFilter(r, groupRoleRequest.Grain, groupRoleRequest.SecurableItem))
                    .Select(r => r.ToRoleApiModel()),
                GroupSource = group.Source
            };

            return groupRoleApiModel;
        }

        public static GroupUserApiModel ToGroupUserApiModel(this Group group)
        {
            var groupUserApiModel = new GroupUserApiModel
            {
                Id = group.Id,
                GroupName = group.Name,
                Users = group.Users?.Where(u => !u.IsDeleted).Select(r => r.ToUserApiModel()),
                GroupSource = group.Source
            };

            return groupUserApiModel;
        }

        public static Group ToGroupDomainModel(this GroupRoleApiModel groupRoleApiModel)
        {
            var group = new Group
            {
                Id = string.IsNullOrEmpty(groupRoleApiModel.Id) ? groupRoleApiModel.GroupName : groupRoleApiModel.Id,
                Name = groupRoleApiModel.GroupName,
                Source = groupRoleApiModel.GroupSource
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

        public static SecurableItemApiModel ToSecurableItemApiModel(this SecurableItem securableItem)
        {
            var securableItemApiModel = new SecurableItemApiModel
            {
                Id = securableItem.Id,
                Name = securableItem.Name,
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
                SecurableItems = securableItem.SecurableItems?.Select(s => s.ToSecurableItemDomainModel()).ToList(),
                CreatedDateTimeUtc = securableItem.CreatedDateTimeUtc,
                CreatedBy = securableItem.CreatedBy,
                ModifiedDateTimeUtc = securableItem.ModifiedDateTimeUtc,
                ModifiedBy = securableItem.ModifiedBy
            };
            return securableItemModel;
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

        public static Error ToError(this IEnumerable<string> errors, string target, HttpStatusCode statusCode)
        {
            var details = errors.Select(e => new Error
            {
                Code = statusCode.ToString(),
                Message = e,
                Target = target
            }).ToList();

            var error = new Error
            {
                Message = details.Count > 1 ? "Multiple Errors" : details.FirstOrDefault()?.Message,
                Details = details.ToArray()
            };

            return error;
        }
    }
}

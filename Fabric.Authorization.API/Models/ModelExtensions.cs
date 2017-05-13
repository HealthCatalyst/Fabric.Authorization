using System;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using FluentValidation.Results;

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
                Permissions = role.Permissions?.Select(p => p.ToPermissionApiModel())
            };
            return roleApiModel;
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

        public static Permission ToPermissionDomainModel(this PermissionApiModel permission)
        {
            var permissionApiModel = new Permission
            {
                Id = permission.Id ?? Guid.Empty,
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
            var clientApiModel = new Client()
            {
                Id = client.Id,
                Name = client.Name,
                CreatedDateTimeUtc = client.CreatedDateTimeUtc,
                ModifiedDateTimeUtc = client.ModifiedDateTimeUtc,
                CreatedBy = client.CreatedBy,
                ModifiedBy = client.ModifiedBy,
                TopLevelSecurableItem = client.TopLevelSecurableItem?.ToSecurableItemDomainModel()

            };
            return clientApiModel;
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
                ModifiedBy = securableItem.ModifiedBy
            };
            return securableItemApiModel;
        }

        public static SecurableItem ToSecurableItemDomainModel(this SecurableItemApiModel securableItem)
        {
            var securableItemApiModel = new SecurableItem
            {
                Id = securableItem.Id ?? Guid.Empty,
                Name = securableItem.Name,
                SecurableItems = securableItem.SecurableItems?.Select(s => s.ToSecurableItemDomainModel()).ToList(),
                CreatedDateTimeUtc = securableItem.CreatedDateTimeUtc,
                CreatedBy = securableItem.CreatedBy,
                ModifiedDateTimeUtc = securableItem.ModifiedDateTimeUtc,
                ModifiedBy = securableItem.ModifiedBy
            };
            return securableItemApiModel;
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
                Message = details.Count > 1 ? "Multiple Errors" : details.FirstOrDefault().Message,
                Details = details.ToArray()
            };

            return error;
        }
    }
}

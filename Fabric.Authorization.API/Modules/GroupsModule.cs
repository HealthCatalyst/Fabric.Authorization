using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class GroupsModule : FabricModule<Group>
    {
        private GroupService _groupService { get; }

        public GroupsModule(GroupService groupService, GroupValidator validator, ILogger logger) : base(
            "/Groups", logger, validator)
        {
            _groupService = groupService;

            base.Post("/", _ => this.AddGroup());
            base.Post("/UpdateGroups", _ => this.UpdateGroupList());
            base.Get("/{groupName}", p => this.GetGroup(p));
            base.Delete("/{groupName}", p => this.DeleteGroup(p));

            base.Get("/{groupName}/roles", _ => this.GetRolesFromGroup());
            base.Post("/{groupName}/roles", p => this.AddRoleToGroup(p));
            base.Delete("/{groupName}/roles", p => this.DeleteRoleFromGroup(p));
        }

        private dynamic GetGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(this.AuthorizationManageClientsClaim, this.AuthorizationReadClaim);
                Group group = this._groupService.GetGroup(parameters.groupName).Result;
                return group.ToGroupRoleApiModel();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Group>)
                {
                    return HttpStatusCode.NotFound;
                }
                else
                {
                    throw;
                }
            }
        }

        private dynamic DeleteGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(this.AuthorizationManageClientsClaim, this.AuthorizationWriteClaim);
                Group group = _groupService.GetGroup(parameters.groupName).Result;
                _groupService.DeleteGroup(group).Wait();
                return HttpStatusCode.NoContent;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Group>)
                {
                    return HttpStatusCode.NotFound;
                }
                else
                {
                    throw;
                }
            }
        }

        private HttpStatusCode AddGroup()
        {
            try
            {
                this.RequiresClaims(this.AuthorizationManageClientsClaim, this.AuthorizationWriteClaim);
                var group = this.Bind<GroupRoleApiModel>();
                _groupService.AddGroup(group.ToGroupDomainModel()).Wait();
                return HttpStatusCode.Created;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is AlreadyExistsException<Group>)
                {
                    return HttpStatusCode.BadRequest;
                }
                else
                {
                    throw;
                }
            }

        }

        private HttpStatusCode UpdateGroupList()
        {
            try
            {
                this.RequiresClaims(this.AuthorizationManageClientsClaim, this.AuthorizationWriteClaim);
                var group = this.Bind<List<GroupRoleApiModel>>();
                _groupService.UpdateGroupList(group.Select(g => g.ToGroupDomainModel())).Wait();
                return HttpStatusCode.NoContent;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is AlreadyExistsException<Group>)
                {
                    return HttpStatusCode.BadRequest;
                }
                else
                {
                    throw;
                }
            }
        }

        private dynamic GetRolesFromGroup()
        {
            try
            {
                this.RequiresClaims(this.AuthorizationReadClaim);
                var groupInfoRequest = this.Bind<GroupInfoRequest>();
                var roles = _groupService.GetRolesForGroup(groupInfoRequest.GroupName, groupInfoRequest.Grain,
                    groupInfoRequest.SecurableItem).Result;

                return new GroupRoleApiModel
                {
                    // RequestedGrain = groupInfoRequest.Grain,
                    // RequestedSecurableItem = groupInfoRequest.SecurableItem,
                    GroupName = groupInfoRequest.GroupName,
                    Roles = roles.Select(r => r.ToRoleApiModel())
                };
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Group>)
                {
                    return HttpStatusCode.NotFound;
                }
                else
                {
                    throw;
                }
            }
        }

        private HttpStatusCode AddRoleToGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(this.AuthorizationWriteClaim);
                var roleApiModel = this.Bind<RoleApiModel>();
                if (roleApiModel.Id == null)
                {
                    throw new NotFoundException<Role>();
                }

                _groupService.AddRoleToGroup(parameters.groupName, roleApiModel.Id.Value).Wait();
                return HttpStatusCode.NoContent;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Group>)
                {
                    return HttpStatusCode.NotFound;
                }
                if (ex.InnerException is NotFoundException<Role>)
                {
                    return HttpStatusCode.BadRequest;
                }
                else
                {
                    throw;
                }
            }
        }

        private HttpStatusCode DeleteRoleFromGroup(dynamic parameters)
        {
            try
            {
                this.RequiresClaims(this.AuthorizationWriteClaim);
                var roleApiModel = this.Bind<RoleApiModel>();
                if (roleApiModel.Id == null)
                {
                    throw new NotFoundException<Role>();
                }

                _groupService.DeleteRoleFromGroup(parameters.groupName, roleApiModel.Id.Value).Wait();
                return HttpStatusCode.NoContent;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Group>)
                {
                    return HttpStatusCode.NotFound;
                }
                if (ex.InnerException is NotFoundException<Role>)
                {
                    return HttpStatusCode.BadRequest;
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
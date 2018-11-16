using System.Linq;
using AutoMapper;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class GroupMapperProfile : Profile
    {
        public GroupMapperProfile()
        {
            CreateMap<Group, Domain.Models.Group>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.GroupId))
                .ForMember(x => x.Roles, opt => opt.MapFrom(src => src.Roles))
                .ForMember(x => x.Users, opt => opt.MapFrom(src => src.Users))
                .ReverseMap()
                .ForMember(x => x.Id, opt => opt.Ignore())
                .ForMember(x => x.GroupRoles, opt => opt.MapFrom(src =>
                    src.Roles.Select(r => new GroupRole
                    {
                        RoleId = r.Id,
                        GroupId = src.Id
                    })))
                .ForMember(x => x.GroupUsers, opt => opt.MapFrom(src =>
                    src.Users.Select(u => new GroupUser
                    {
                        SubjectId = u.SubjectId,
                        IdentityProvider = u.IdentityProvider,
                        GroupId = src.Id
                    })))
                .ForMember(x => x.ChildGroups, opt => opt.MapFrom(src =>
                    src.Children.Select(c => new ChildGroup
                    {
                        ParentGroupId = src.Id,
                        ChildGroupId = c.Id
                    })))
                .ForMember(x => x.ChildGroups, opt => opt.MapFrom(src =>
                    src.Parents.Select(p => new ChildGroup
                    {
                        ParentGroupId = p.Id,
                        ChildGroupId = src.Id
                    })));
        }
    }
}
using System.Linq;
using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class RoleMapperProfile : Profile
    {
        public RoleMapperProfile()
        {
            CreateMap<EntityModels.Role, Domain.Models.Role>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.RoleId))
                .ForMember(x => x.ParentRole, opt => opt.MapFrom(src => src.ParentRole.RoleId))
                .ForMember(x => x.Groups, opt => opt.MapFrom(src => src.Groups.Select(g => g.Name)))
                .ForMember(x => x.ChildRoles, opt => opt.MapFrom(src => src.ChildRoles.Select(cr => cr.RoleId)))
                .ForMember(x => x.Permissions, opt => opt.MapFrom(src => src.AllowedPermissions))
                .ForMember(x => x.DeniedPermissions, opt => opt.MapFrom(src => src.DeniedPermissions))
                .ReverseMap();

            CreateMap<EntityModels.Role, Domain.Models.Role>()
                .ForMember(x => x.SecurableItem, opt => opt.MapFrom(src => src.SecurableItem.Name));
        }
    }
}
using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class RoleMapperProfile : Profile
    {
        public RoleMapperProfile()
        {
            CreateMap<EntityModels.Role, Domain.Models.Role>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.RoleId))
                .ReverseMap();

            CreateMap<EntityModels.Role, Domain.Models.Role>()
                .ForMember(x => x.SecurableItem, opt => opt.MapFrom(src => src.SecurableItem.Name));
        }
    }
}
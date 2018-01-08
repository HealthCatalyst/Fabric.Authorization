using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class SecurableItemMapperProfile : Profile
    {
        public SecurableItemMapperProfile()
        {
            CreateMap<EntityModels.SecurableItem, Domain.Models.SecurableItem>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.SecurableItemId))
                .ReverseMap()
                .ForMember(x => x.SecurableItemId, opt => opt.MapFrom(src => src.Id))
                .ForMember(x => x.Id, opt => opt.Ignore())
                .ForMember(x => x.Parent, src => src.Ignore());
        }
    }
}

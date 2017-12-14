using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class SecurableItemMapperProfile : Profile
    {
        public SecurableItemMapperProfile()
        {
            CreateMap<EntityModels.SecurableItem, Domain.Models.SecurableItem>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.SecurableItemId))
                .ReverseMap();
        }
    }
}

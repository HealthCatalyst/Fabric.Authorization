using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class SecurableItemMapperProfile : Profile
    {
        public SecurableItemMapperProfile()
        {
            CreateMap<EntityModels.SecurableItem, Domain.Models.SecurableItem>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.SecurableItemId))
                .ForMember(x => x.SecurableItems, opt => opt.MapFrom(src => src.SecurableItems))
                .ReverseMap()
                .ForMember(x => x.SecurableItemId, opt => opt.MapFrom(src => src.Id))
                .ForMember(x => x.SecurableItems, opt => opt.MapFrom(src => src.SecurableItems))
                .ForMember(x => x.Id, opt => opt.Ignore())
                .ForMember(x => x.Parent, opt => opt.Ignore())
                .ForMember(x => x.ParentId, opt => opt.Ignore())
                .ForMember(x => x.Client, opt => opt.Ignore())
                .ForMember(x => x.Permissions, opt => opt.Ignore())
                .ForMember(x => x.Roles, opt => opt.Ignore());

        }
    }
}

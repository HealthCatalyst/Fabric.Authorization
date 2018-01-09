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
                .ForPath(x => x.SecurableItemId, opt => opt.MapFrom(src => src.Id))
                .ForMember(x => x.SecurableItems, opt => opt.MapFrom(src => src.SecurableItems))
                .ForMember(x => x.Id, opt => opt.Ignore())
                .ForPath(x => x.Id, opt => opt.Ignore())
                .ForPath(x => x.Parent, opt => opt.Ignore())
                .ForPath(x => x.ParentId, opt => opt.Ignore())
                .ForPath(x => x.Client, opt => opt.Ignore())
                .ForPath(x => x.Permissions, opt => opt.Ignore())
                .ForPath(x => x.Roles, opt => opt.Ignore());
        }
    }
}

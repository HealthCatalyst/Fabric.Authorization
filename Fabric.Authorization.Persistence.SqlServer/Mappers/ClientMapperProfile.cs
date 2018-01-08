using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class ClientMapperProfile : Profile
    {
        public ClientMapperProfile()
        {
            //entity to model 
            CreateMap<EntityModels.Client, Domain.Models.Client>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.ClientId))
                .ForMember(x => x.TopLevelSecurableItem, opt => opt.Ignore())
                .ReverseMap()
                .ForPath(x => x.ClientId, opt => opt.MapFrom(x => x.Id))
                .ForMember(x => x.Id, opt => opt.Ignore())
                .ForMember(x => x.TopLevelSecurableItem, opt => opt.Ignore());
        }
    }
}

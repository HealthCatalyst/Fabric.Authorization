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
                .ReverseMap();

            //model to entity
            //CreateMap<Domain.Models.Client, EntityModels.Client>()
            //    .ForMember(x => x.ClientId, opt => opt.MapFrom(src => src.Id));
        }
    }
}

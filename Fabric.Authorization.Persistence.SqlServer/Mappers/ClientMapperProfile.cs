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
                .ReverseMap()
                .ForPath(x => x.ClientId, opt => opt.MapFrom(x => x.Id));
        }
    }
}

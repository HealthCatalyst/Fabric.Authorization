using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class UserMapperProfile : Profile
    {
        public UserMapperProfile()
        {
            CreateMap<EntityModels.User, Domain.Models.User>()
                .ReverseMap();
        }
    }
}
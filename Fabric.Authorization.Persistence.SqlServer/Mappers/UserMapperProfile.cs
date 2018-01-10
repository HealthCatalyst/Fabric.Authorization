using AutoMapper;
using Fabric.Authorization.Domain.Models.Formatters;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class UserMapperProfile : Profile
    {
        public UserMapperProfile()
        {
            CreateMap<EntityModels.User, Domain.Models.User>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => new UserIdentifierFormatter().Format(src)))
                .ReverseMap()
                .ForMember(x => x.Id, opt => opt.Ignore());
        }
    }
}
using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class GroupMapperProfile : Profile
    {
        public GroupMapperProfile()
        {
            CreateMap<EntityModels.Group, Domain.Models.Group>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.GroupId))
                .ReverseMap()
                .ForMember(x => x.Id, opt => opt.Ignore());
        }
    }
}
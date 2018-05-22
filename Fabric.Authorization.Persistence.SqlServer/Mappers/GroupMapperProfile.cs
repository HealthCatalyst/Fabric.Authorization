using AutoMapper;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class GroupMapperProfile : Profile
    {
        public GroupMapperProfile()
        {
            CreateMap<Group, Domain.Models.Group>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.GroupId))
                .ForMember(x => x.Roles, opt => opt.MapFrom(src => src.Roles))
                .ForMember(x => x.Users, opt => opt.MapFrom(src => src.Users))
                .ReverseMap()
                .ForMember(x => x.Id, opt => opt.Ignore());
        }
    }
}
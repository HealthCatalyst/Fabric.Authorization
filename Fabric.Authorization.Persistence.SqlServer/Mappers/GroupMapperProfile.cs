using System.Linq;
using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class GroupMapperProfile : Profile
    {
        public GroupMapperProfile()
        {
            CreateMap<EntityModels.Group, Domain.Models.Group>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.GroupId))
                .ForMember(x => x.Roles, opt => opt.MapFrom(src => src.GroupRoles.Select(gr => gr.Role)))
                .ForMember(x => x.Users, opt => opt.MapFrom(src => src.GroupUsers.Select(gu => gu.User)))
                .ReverseMap()
                .ForMember(x => x.Id, opt => opt.Ignore());
        }
    }
}
using System.Linq;
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
                .ForMember(x => x.Groups, opt => opt.MapFrom(src => src.GroupUsers.Select(gu => gu.User)))
                .ForMember(x => x.Permissions, opt => opt.MapFrom(src => src.UserPermissions.Select(up => up.User)))
                .ReverseMap()
                .ForMember(x => x.Id, opt => opt.Ignore());
        }
    }
}
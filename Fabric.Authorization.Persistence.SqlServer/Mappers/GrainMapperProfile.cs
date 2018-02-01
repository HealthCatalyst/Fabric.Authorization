using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Internal;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class GrainMapperProfile : Profile
    {
        private readonly char _separator = ';';
        public GrainMapperProfile()
        {
            //entity to model
            CreateMap<EntityModels.Grain, Domain.Models.Grain>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.GrainId))
                .ForMember(x => x.RequiredWriteScopes, opt => opt.MapFrom(src => src.RequiredWriteScopes.Split(_separator)))
                .ReverseMap()
                .ForMember(x => x.RequiredWriteScopes, opt => opt.MapFrom(src => src.RequiredWriteScopes.Join(_separator.ToString())));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public class SecurableItemMapperProfile : Profile
    {
        public SecurableItemMapperProfile()
        {
            CreateMap<EntityModels.SecurableItem, SecurableItem>()
                .ForMember(x => x.Id, opt => opt.MapFrom(src => src.SecurableItemId))
                .ForMember(x => x.SecurableItems, opt => opt.MapFrom(src => src.SecurableItems))
                .ForMember(x => x.Grain,
                    opt => opt.ResolveUsing((entityModel, domainModel, destMember) => entityModel?.Grain?.Name))
                .ReverseMap()
                .ForMember(x => x.SecurableItemId, opt => opt.MapFrom(src => src.Id))
                .ForMember(x => x.SecurableItems, opt => opt.MapFrom(src => src.SecurableItems))
                .ForMember(x => x.Id, opt => opt.Ignore())
                .ForMember(x => x.Parent, opt => opt.Ignore())
                .ForMember(x => x.ParentId, opt => opt.Ignore())
                .ForMember(x => x.Client, opt => opt.Ignore())
                .ForMember(x => x.Permissions, opt => opt.Ignore())
                .ForMember(x => x.Roles, opt => opt.Ignore())
                //.ForMember(x => x.GrainId, opt => opt.ResolveUsing<GrainNameToEntityIdResolver>())
                .ForMember(x => x.Grain, opt => opt.Ignore());
        }
    }

    public class GrainNameToEntityIdResolver : IValueResolver<SecurableItem, EntityModels.SecurableItem, Guid?>
    {
        private readonly IEnumerable<EntityModels.Grain> _grains;

        public GrainNameToEntityIdResolver(IEnumerable<EntityModels.Grain> grains)
        {
            _grains = grains;
        }

        public Guid? Resolve(SecurableItem source, EntityModels.SecurableItem destination, Guid? destMember, ResolutionContext context)
        {
            var grain = _grains.FirstOrDefault(g => g.Name == source.Grain);
            return grain?.GrainId;
        }
    }
}

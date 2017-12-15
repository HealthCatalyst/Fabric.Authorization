using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public static class GroupMapper
    {
        internal static IMapper Mapper { get; }

        static GroupMapper()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<GroupMapperProfile>())
                .CreateMapper();
        }

        public static Domain.Models.Group ToModel(this EntityModels.Group entity)
        {
            return entity == null ? null : Mapper.Map<Domain.Models.Group>(entity);
        }

        public static EntityModels.Group ToEntity(this Domain.Models.Group model)
        {
            return model == null ? null : Mapper.Map<EntityModels.Group>(model);
        }

        public static void ToEntity(this Domain.Models.Group model, EntityModels.Group entity)
        {
            Mapper.Map(model, entity);
        }
    }
}
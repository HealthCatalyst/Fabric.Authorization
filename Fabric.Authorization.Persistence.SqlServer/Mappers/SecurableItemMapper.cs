using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public static class SecurableItemMapper
    {
        internal static IMapper Mapper { get; }

        static SecurableItemMapper()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<SecurableItemMapperProfile>())
                .CreateMapper();
        }

        public static Domain.Models.SecurableItem ToModel(this EntityModels.SecurableItem entity)
        {
            return entity == null ? null : Mapper.Map<Domain.Models.SecurableItem>(entity);
        }

        public static EntityModels.SecurableItem ToEntity(this Domain.Models.SecurableItem model)
        {
            return model == null ? null : Mapper.Map<EntityModels.SecurableItem>(model);
        }

        public static void ToEntity(this Domain.Models.SecurableItem model, EntityModels.SecurableItem entity)
        {
            Mapper.Map(model, entity);
        }

    }
}

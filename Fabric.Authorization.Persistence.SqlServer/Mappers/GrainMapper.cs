using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public static class GrainMapper
    {
        public static Domain.Models.Grain ToModel(this EntityModels.Grain entity)
        {
            return entity == null ? null : Mapper.Map<Domain.Models.Grain>(entity);
        }

        public static EntityModels.Grain ToEntity(this Domain.Models.Grain model)
        {
            return model == null ? null : Mapper.Map<EntityModels.Grain>(model);
        }

        public static void ToEntity(this Domain.Models.Grain model, EntityModels.Grain entity)
        {
            Mapper.Map(model, entity);
        }
    }
}

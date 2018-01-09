using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public static class PermissionMapper
    {
        public static Domain.Models.Permission ToModel(this EntityModels.Permission entity)
        {
            return entity == null ? null : Mapper.Map<Domain.Models.Permission>(entity);
        }

        public static EntityModels.Permission ToEntity(this Domain.Models.Permission model)
        {
            return model == null ? null : Mapper.Map<EntityModels.Permission>(model);
        }

        public static void ToEntity(this Domain.Models.Permission model, EntityModels.Permission entity)
        {
            Mapper.Map(model, entity);
        }
    }
}
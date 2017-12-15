using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public static class RoleMapper
    {
        internal static IMapper Mapper { get; }

        static RoleMapper()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<RoleMapperProfile>())
                .CreateMapper();
        }

        public static Domain.Models.Role ToModel(this EntityModels.Role entity)
        {
            return entity == null ? null : Mapper.Map<Domain.Models.Role>(entity);
        }

        public static EntityModels.Role ToEntity(this Domain.Models.Role model)
        {
            return model == null ? null : Mapper.Map<EntityModels.Role>(model);
        }

        public static void ToEntity(this Domain.Models.Role model, EntityModels.Role entity)
        {
            Mapper.Map(model, entity);
        }
    }
}
using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public static class UserMapper
    {
        internal static IMapper Mapper { get; }

        static UserMapper()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<UserMapperProfile>())
                .CreateMapper();
        }

        public static Domain.Models.User ToModel(this EntityModels.User entity)
        {
            return entity == null ? null : Mapper.Map<Domain.Models.User>(entity);
        }

        public static EntityModels.User ToEntity(this Domain.Models.User model)
        {
            return model == null ? null : Mapper.Map<EntityModels.User>(model);
        }

        public static void ToEntity(this Domain.Models.User model, EntityModels.User entity)
        {
            Mapper.Map(model, entity);
        }
    }
}
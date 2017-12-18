using AutoMapper;

namespace Fabric.Authorization.Persistence.SqlServer.Mappers
{
    public static class ClientMapper
    {
        internal static IMapper Mapper { get; }

        static ClientMapper()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<ClientMapperProfile>())
                .CreateMapper();
        }

        public static Domain.Models.Client ToModel(this EntityModels.Client entity)
        {
            return entity == null ? null : Mapper.Map<Domain.Models.Client>(entity);
        }

        public static EntityModels.Client ToEntity(this Domain.Models.Client model)
        {
            return model == null ? null : Mapper.Map<EntityModels.Client>(model);
        }

        public static void ToEntity(this Domain.Models.Client model, EntityModels.Client entity)
        {
            Mapper.Map(model, entity);                      
        }
    }
}

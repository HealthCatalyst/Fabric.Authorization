using System.Linq;
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
            
            foreach (var securableItem in model.TopLevelSecurableItem.SecurableItems)
            {
                var existingItem = entity.TopLevelSecurableItem.SecurableItems
                    .FirstOrDefault(s => s.SecurableItemId.Equals(securableItem.Id));

                if (existingItem != null)
                {
                    existingItem.Name = securableItem.Name;
                }
                else
                {
                    entity.TopLevelSecurableItem.SecurableItems.Add(new EntityModels.SecurableItem
                    {
                        SecurableItemId = securableItem.Id,
                        Name = securableItem.Name
                    });
                }

            }
        }
    }
}

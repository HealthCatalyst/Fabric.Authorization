using Swagger.ObjectModel;

namespace Fabric.Authorization.API.Swagger
{
    public static class Parameters
    {
        public static readonly Parameter GrainParameter = new Parameter
        {
            Name = "grain",
            Description = "The top level grain to return permissions for",
            Required = true,
            Type = "string",
            In = ParameterIn.Path
        };

        public static readonly Parameter SecurableItemParameter = new Parameter
        {
            Name = "securableItem",
            Description = "The specific securableItem within the grain to return permissions for",
            Required = true,
            Type = "string",
            In = ParameterIn.Path
        };
    }
}
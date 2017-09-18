using Nancy.Swagger;
using Nancy.Swagger.Services;
using Swagger.ObjectModel;

namespace Fabric.Authorization.API.Modules
{
    public abstract class SearchMetadataModule : BaseMetadataModule
    {
        protected readonly Parameter PageNumberParameter = new Parameter
        {
            Name = "page_number",
            Description = "Page number",
            Required = false,
            Type = "int",
            In = ParameterIn.Query
        };

        protected readonly Parameter PageSizeParameter = new Parameter
        {
            Name = "page_size",
            Description = "Page size",
            Required = false,
            Type = "int",
            In = ParameterIn.Query
        };

        protected readonly Parameter FilterParameter = new Parameter
        {
            Name = "filter",
            Description = "Text filter",
            Required = false,
            Type = "string",
            In = ParameterIn.Query
        };

        protected readonly Parameter SortKeyParameter = new Parameter
        {
            Name = "sort_key",
            Description = "Sort key",
            Required = false,
            Type = "string",
            In = ParameterIn.Query
        };

        protected readonly Parameter SortDirectionParameter = new Parameter
        {
            Name = "sort_dir",
            Description = "Sort direction",
            Required = false,
            Type = "string",
            In = ParameterIn.Query
        };

        protected SearchMetadataModule(ISwaggerModelCatalog modelCatalog, ISwaggerTagCatalog tagCatalog) :
            base(modelCatalog, tagCatalog)
        {
        }
    }
}
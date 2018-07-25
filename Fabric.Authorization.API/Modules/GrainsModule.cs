using Fabric.Authorization.API.Services;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Nancy.Security;
using Serilog;
using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Validators;

namespace Fabric.Authorization.API.Modules
{
    public class GrainsModule : FabricModule<Grain>
    {
        private readonly GrainService _grainService;

        public GrainsModule(GrainService grainService,
            GrainValidator validator,
            AccessService accessService,
            ILogger logger) : base("/v1/grains", logger, validator, accessService)
        {
            _grainService = grainService ??
                           throw new ArgumentNullException(nameof(grainService));

            Get("/",
                async _ => await GetGrain().ConfigureAwait(false),
                null,
                "GetGrain");
        }

        private async Task<dynamic> GetGrain()
        {
            CheckReadAccess();
            return (await _grainService.GetAllGrains()).ToGrainApiModels();
        }
    }
}

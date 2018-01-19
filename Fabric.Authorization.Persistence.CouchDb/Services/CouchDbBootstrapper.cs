using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.CouchDb.Stores;
using Serilog;

namespace Fabric.Authorization.Persistence.CouchDb.Services
{
    public class CouchDbBootstrapper : IDbBootstrapper
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly ILogger _logger;

        public CouchDbBootstrapper(IDocumentDbService documentDbService, ILogger logger)
        {
            _documentDbService = documentDbService ?? throw new ArgumentNullException(nameof(documentDbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Setup()
        {
            var initializeTask = Task.Run(async () => { await _documentDbService.Initialize(); });
            initializeTask.Wait();
            var setupUserTask = Task.Run(async () => { await _documentDbService.SetupDefaultUser(); });
            setupUserTask.Wait();
            var addViewsTask = Task.Run(async () =>
                {
                    await _documentDbService.AddViews("roles", CouchDbRoleStore.GetViews());
                });
            addViewsTask.Wait();
            var addPermissionViewsTask = Task.Run(async () =>
                {
                    await _documentDbService.AddViews("permissions", CouchDbPermissionStore.GetViews());
                });
            addPermissionViewsTask.Wait();
        }
    }
}
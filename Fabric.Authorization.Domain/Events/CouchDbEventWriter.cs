using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Events
{
    public class CouchDbEventWriter : IEventWriter
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly IEventWriter _innerEventWriter;

        public CouchDbEventWriter(IDocumentDbService documentDbService, IEventWriter innerEventWriter)
        {
            _documentDbService = documentDbService ?? throw new ArgumentNullException(nameof(documentDbService));
            _innerEventWriter = innerEventWriter ?? throw new ArgumentNullException(nameof(innerEventWriter));
        }

        public async Task WriteEvent(Event evt)
        {
            await _innerEventWriter.WriteEvent(evt);
            await _documentDbService.AddDocument(evt.Identifier, evt).ConfigureAwait(false);
        }
    }
}
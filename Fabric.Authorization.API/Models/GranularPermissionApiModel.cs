using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.API.Models
{
    public class GranularPermissionApiModel : IIdentifiable, ITrackable
    {
        public string Id { get; set; }
        public string Target { get; set; }
        public IEnumerable<PermissionApiModel> Permissions { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public string Identifier => this.Id ?? "";
    }
}
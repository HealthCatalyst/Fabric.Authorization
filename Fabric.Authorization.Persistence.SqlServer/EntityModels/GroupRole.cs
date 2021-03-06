﻿using System;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class GroupRole : ITrackable, ISoftDelete
    {
        public int Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid RoleId { get; set; }

        public bool IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string ModifiedBy { get; set; }

        public Group Group { get; set; }
        public Role Role { get; set; }
    }
}
using System;

namespace Fabric.Authorization.Domain.Models
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}

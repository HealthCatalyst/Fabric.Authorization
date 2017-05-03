using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.Models
{
    public interface IIdentifiable
    {
        Guid? Id { get; }
    }
}

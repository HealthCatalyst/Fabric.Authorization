using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Models
{
    public interface IUser
    {
        string SubjectId { get; set; }
        string IdentityProvider { get; set; }
    }
}

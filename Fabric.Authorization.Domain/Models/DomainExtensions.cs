using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Models
{
    public static class DomainExtensions
    {
        public static void Track(this ITrackable model, bool creation = true, string user = null)
        {
            if (creation)
            {
                if (user != null)
                {
                    model.CreatedBy = user;
                }

                model.CreatedDateTimeUtc = DateTime.UtcNow;
            }
            else
            {
                if (user != null)
                {
                    model.ModifiedBy = user;
                }

                model.ModifiedDateTimeUtc = DateTime.UtcNow;
            }
        }
    }
}

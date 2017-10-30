using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        public static IEnumerable<T> Track<T>(this IEnumerable<T> models, bool creation = true, string user = null)
            where T : ITrackable
        {
            return models.Select(m =>
            {
                Track(m, creation, user);
                return m;
            });
        }

        public static string ToString(this IEnumerable list, string separator)
        {
            return string.Join(separator, list);
        }
    }
}
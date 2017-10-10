using System;
using System.Collections;
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

        public static string ListToString(this IEnumerable list)
        {
            return list.ListToString(Environment.NewLine);
        }

        public static string ListToString(this IEnumerable list, string lineBreak)
        {
            var sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.Append(item);
                sb.Append(lineBreak);
            }

            return sb.ToString();
        }
    }
}
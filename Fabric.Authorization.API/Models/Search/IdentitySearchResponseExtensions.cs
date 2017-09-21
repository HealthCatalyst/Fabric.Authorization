using System;
using System.Collections.Generic;
using System.Linq;

namespace Fabric.Authorization.API.Models.Search
{
    public static class IdentitySearchResponseExtensions
    {
        public static IOrderedEnumerable<IdentitySearchResponse> Sort(this IEnumerable<IdentitySearchResponse> results,
            IdentitySearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SortKey))
            {
                return results.OrderBy(r => r.SubjectId);
            }

            var isAscending = string.IsNullOrWhiteSpace(request.SortDirection) ||
                              SearchConstants.AscendingSortKeys.Contains(request.SortDirection);

            switch (request.SortKey.ToLower())
            {
                case "name":
                    return isAscending ? results.OrderBy(r => r.Name) : results.OrderByDescending(r => r.Name);

                case "role":
                    return isAscending ? results.OrderBy(r => r.Name) : results.OrderByDescending(r => r.Name);

                case "lastlogin":
                    return isAscending ? results.OrderBy(r => r.Name) : results.OrderByDescending(r => r.Name);

                default:
                    return isAscending
                        ? results.OrderBy(r => r.SubjectId)
                        : results.OrderByDescending(r => r.SubjectId);
            }
        }

        public static IEnumerable<IdentitySearchResponse> Filter(this IEnumerable<IdentitySearchResponse> results,
            IdentitySearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Filter))
            {
                return results;
            }

            var filter = request.Filter.ToLower();

            return results.Where(r =>
                (!string.IsNullOrWhiteSpace(r.Name) && r.Name.ToLower().Contains(filter))
                || (!string.IsNullOrWhiteSpace(r.SubjectId) && r.SubjectId.ToLower().Contains(filter))
                || r.Roles.Contains(filter, StringComparer.OrdinalIgnoreCase));
        }
    }
}
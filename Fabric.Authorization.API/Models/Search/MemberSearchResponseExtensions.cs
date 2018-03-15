using System;
using System.Collections.Generic;
using System.Linq;

namespace Fabric.Authorization.API.Models.Search
{
    public static class MemberSearchResponseExtensions
    {
        public static IOrderedEnumerable<MemberSearchResponse> Sort(this IEnumerable<MemberSearchResponse> results,
            MemberSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SortKey))
            {
                return results.OrderBy(r => r.SubjectId);
            }

            var isAscending = string.IsNullOrWhiteSpace(request.SortDirection) ||
                              SearchConstants.AscendingSortKeys.Contains(request.SortDirection);

            switch (request.SortKey.ToLower())
            {
                case "subjectid":
                    return isAscending ? results.OrderBy(r => r.SubjectId) : results.OrderByDescending(r => r.SubjectId);

                case "idp":
                    return isAscending ? results.OrderBy(r => r.IdentityProvider) : results.OrderByDescending(r => r.IdentityProvider);

                case "name":
                    return isAscending ? results.OrderBy(r => r.Name) : results.OrderByDescending(r => r.Name);

                case "firstname":
                    return isAscending ? results.OrderBy(r => r.FirstName) : results.OrderByDescending(r => r.FirstName);

                case "middlename":
                    return isAscending ? results.OrderBy(r => r.MiddleName) : results.OrderByDescending(r => r.MiddleName);

                case "lastname":
                    return isAscending ? results.OrderBy(r => r.LastName) : results.OrderByDescending(r => r.LastName);

                case "groupname":
                    return isAscending ? results.OrderBy(r => r.GroupName) : results.OrderByDescending(r => r.GroupName);

                case "lastlogin":
                    return isAscending ? results.OrderBy(r => r.LastLoginDateTimeUtc) : results.OrderByDescending(r => r.LastLoginDateTimeUtc);

                default:
                    return isAscending
                        ? results.OrderBy(r => r.SubjectId)
                        : results.OrderByDescending(r => r.SubjectId);
            }
        }

        public static IEnumerable<MemberSearchResponse> Filter(this IEnumerable<MemberSearchResponse> results,
            MemberSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Filter))
            {
                return results;
            }

            var filter = request.Filter.ToLower();

            return results.Where(r =>
                (!string.IsNullOrWhiteSpace(r.Name) && r.Name.ToLower().Contains(filter))
                || (!string.IsNullOrWhiteSpace(r.SubjectId) && r.SubjectId.ToLower().Contains(filter))
                || r.Roles.Select(role => role.Name).Contains(filter, StringComparer.OrdinalIgnoreCase));
        }
    }
}
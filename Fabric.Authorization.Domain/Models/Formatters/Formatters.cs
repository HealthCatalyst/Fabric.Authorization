using System;

namespace Fabric.Authorization.Domain.Models.Formatters
{
    public class UserIdentifierFormatter : IIdentifierFormatter<User>
    {
        public string Format(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "user cannot be null");
            }

            return !string.IsNullOrWhiteSpace(user.IdentityProvider)
                ? $"{user.SubjectId}:{user.IdentityProvider}"
                : user.SubjectId;
        }
    }
}
using System;

namespace Fabric.Authorization.Domain.Models.Formatters
{
    public class UserIdentifierFormatter : IIdentifierFormatter<IUser>
    {
        public string Format(IUser user)
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
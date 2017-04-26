namespace Fabric.Authorization.Domain.Users
{
    public interface IUserStore
    {
        User GetUser(string userId);

        void AddUser(User user);

        void UpdateUser(User user);
    }
}

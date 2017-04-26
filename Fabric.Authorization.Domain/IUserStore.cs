namespace Fabric.Authorization.Domain
{
    public interface IUserStore
    {
        User GetUser(string userId);

        void AddUser(User user);

        void UpdateUser(User user);
    }
}

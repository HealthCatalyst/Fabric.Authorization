using System.Threading.Tasks;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Services
{
    public interface IAuthorizationDbContext
    {
        DbSet<Client> Clients { get; set; }

        Task<int> SaveChangesAsync();
        int SaveChanges();
    }
}
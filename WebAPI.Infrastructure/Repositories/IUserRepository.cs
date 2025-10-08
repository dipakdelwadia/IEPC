using WebAPI.Domain.Entities;

namespace WebAPI.Infrastructure.Repositories
{
    public interface IUserRepository
    {
        Task <User?> GetByUsername(string username, string clientId = null);
        //void Add(User user);
    }
}
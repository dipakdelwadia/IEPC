using WebAPI.Domain.Entities;

namespace WebAPI.Infrastructure.Repositories
{
    public interface IUserRepository
    {
        Task <User?> GetByUsername(string username, string clientId);
        //void Add(User user);
    }
}
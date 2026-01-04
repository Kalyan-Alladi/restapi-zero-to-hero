using DemoCiCdAzureApi.Entities;

namespace DemoCiCdAzureApi.Repositories
{
    public interface IUserRepository
    {
        // Define methods for user data access
        Task<User?> GetUserByIdAsync(int id);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(int id);
    }
}
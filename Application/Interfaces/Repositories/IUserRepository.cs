using Domain.Entities;



namespace Application.Interfaces.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        Task SoftDeleteUserAsync(int userId, CancellationToken cancellationToken);
        Task HardDeleteUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> UndeleteUserAsync(int userId, CancellationToken cancellationToken = default);
    }
}

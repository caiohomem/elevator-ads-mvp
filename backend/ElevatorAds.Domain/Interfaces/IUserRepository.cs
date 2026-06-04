using ElevatorAds.Domain.Entities;

namespace ElevatorAds.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task AddAsync(User user);
}

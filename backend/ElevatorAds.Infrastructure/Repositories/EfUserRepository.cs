using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Interfaces;
using ElevatorAds.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Infrastructure.Repositories;

public sealed class EfUserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public EfUserRepository(AppDbContext context) => _context = context;

    public Task<User?> GetByUsernameAsync(string username) =>
        _context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Username == username);

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }
}

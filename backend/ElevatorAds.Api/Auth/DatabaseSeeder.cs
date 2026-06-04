using ElevatorAds.Application.Auth;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Api.Auth;

public static class DatabaseSeeder
{
    public static async Task SeedAdminUserAsync(IServiceProvider services, ILogger logger, CancellationToken cancellationToken = default)
    {
        var env = services.GetRequiredService<IHostEnvironment>();
        var config = services.GetRequiredService<IConfiguration>();

        var seedUsername = Environment.GetEnvironmentVariable("SEED_ADMIN_USERNAME")
            ?? config["Auth:SeedAdminUsername"];
        var seedPassword = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD")
            ?? config["Auth:SeedAdminPassword"];

        if (string.IsNullOrWhiteSpace(seedUsername) || string.IsNullOrWhiteSpace(seedPassword))
        {
            if (env.IsDevelopment())
            {
                logger.LogWarning(
                    "No SEED_ADMIN_USERNAME/SEED_ADMIN_PASSWORD configured. The auth API will start with no users; create one manually or set the env vars.");
            }

            return;
        }

        using var scope = services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        var existing = await userRepository.GetByUsernameAsync(seedUsername);
        if (existing is not null)
        {
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = seedUsername,
            PasswordHash = PasswordHasher.Hash(seedPassword),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded admin user '{Username}'.", seedUsername);
    }
}

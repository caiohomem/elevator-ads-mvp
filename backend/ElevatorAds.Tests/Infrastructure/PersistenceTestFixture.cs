using ElevatorAds.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElevatorAds.Tests.Infrastructure;

public sealed class PersistenceTestFixture : IDisposable
{
    public AppDbContext Context { get; }

    public PersistenceTestFixture()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ElevatorAds-Tests-{Guid.NewGuid():N}")
            .Options;
        Context = new AppDbContext(options);
    }

    public void Dispose() => Context.Dispose();
}

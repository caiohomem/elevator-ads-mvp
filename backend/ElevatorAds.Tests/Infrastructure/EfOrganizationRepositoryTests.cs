using ElevatorAds.Domain.Entities;
using ElevatorAds.Infrastructure.Repositories;
using ElevatorAds.Domain.Common;

namespace ElevatorAds.Tests.Infrastructure;

public class EfOrganizationRepositoryTests
{
    [Fact]
    public async Task AddAndRetrieve_PersistsOrganization()
    {
        using var fixture = new PersistenceTestFixture();
        var repository = new EfOrganizationRepository(fixture.Context);

        var now = DateTime.UtcNow;
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Acme Networks",
            Slug = "acme-networks",
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(org);
        var retrieved = await repository.GetByIdAsync(org.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(org.Name, retrieved!.Name);
        Assert.Equal(org.Slug, retrieved.Slug);
        Assert.Equal(org.Status, retrieved.Status);
    }

    [Fact]
    public async Task GetBySlug_ReturnsOrganizationWhenSlugMatches()
    {
        using var fixture = new PersistenceTestFixture();
        var repository = new EfOrganizationRepository(fixture.Context);

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Beta Media",
            Slug = "beta-media",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await repository.AddAsync(org);

        var retrieved = await repository.GetBySlugAsync("beta-media");

        Assert.NotNull(retrieved);
        Assert.Equal(org.Id, retrieved!.Id);
    }

    [Fact]
    public async Task SlugExistsAsync_ReturnsTrueForExistingSlug()
    {
        using var fixture = new PersistenceTestFixture();
        var repository = new EfOrganizationRepository(fixture.Context);

        await repository.AddAsync(new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Gamma Holdings",
            Slug = "gamma-holdings",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        Assert.True(await repository.SlugExistsAsync("gamma-holdings"));
        Assert.False(await repository.SlugExistsAsync("does-not-exist"));
    }

    [Fact]
    public async Task SlugExistsAsync_ExcludesSelfWhenUpdating()
    {
        using var fixture = new PersistenceTestFixture();
        var repository = new EfOrganizationRepository(fixture.Context);

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Delta Group",
            Slug = "delta-group",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await repository.AddAsync(org);

        Assert.True(await repository.SlugExistsAsync("delta-group"));
        Assert.False(await repository.SlugExistsAsync("delta-group", excludeId: org.Id));
    }

    [Fact]
    public async Task EnsureDefaultOrganizationIdAsync_CreatesOrganizationOnFirstCall()
    {
        using var fixture = new PersistenceTestFixture();
        var repository = new EfOrganizationRepository(fixture.Context);

        var orgId = await repository.EnsureDefaultOrganizationIdAsync("Default Organization", "default");

        var retrieved = await repository.GetByIdAsync(orgId);
        Assert.NotNull(retrieved);
        Assert.Equal("default", retrieved!.Slug);
        Assert.Equal("Default Organization", retrieved.Name);
    }

    [Fact]
    public async Task EnsureDefaultOrganizationIdAsync_ReturnsExistingOrganizationIdOnSubsequentCalls()
    {
        using var fixture = new PersistenceTestFixture();
        var repository = new EfOrganizationRepository(fixture.Context);

        var firstId = await repository.EnsureDefaultOrganizationIdAsync("Default Organization", "default");
        var secondId = await repository.EnsureDefaultOrganizationIdAsync("Default Organization", "default");

        Assert.Equal(firstId, secondId);

        var totalOrgs = (await repository.GetAllAsync()).Count();
        Assert.Equal(1, totalOrgs);
    }
}

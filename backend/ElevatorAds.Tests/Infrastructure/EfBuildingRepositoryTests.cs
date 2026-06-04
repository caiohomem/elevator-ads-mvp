using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Infrastructure.Repositories;

namespace ElevatorAds.Tests.Infrastructure;

public class EfBuildingRepositoryTests
{
    [Fact]
    public async Task AddAndRetrieve_PersistsBuilding()
    {
        using var fixture = new PersistenceTestFixture();
        var repository = new EfBuildingRepository(fixture.Context);

        var now = DateTime.UtcNow;
        var building = new Building
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Tower One",
            Address = "Rua A",
            City = "Lisbon",
            Country = "PT",
            PostalCode = "1000-001",
            BuildingType = BuildingType.Corporate,
            EstimatedDailyAudience = 250,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(building);
        var retrieved = await repository.GetByIdAsync(building.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(building.Name, retrieved!.Name);
        Assert.Equal(building.BuildingType, retrieved.BuildingType);
        Assert.Equal(building.EstimatedDailyAudience, retrieved.EstimatedDailyAudience);
    }

    [Fact]
    public async Task Update_UpdatesExistingBuilding()
    {
        using var fixture = new PersistenceTestFixture();
        var repository = new EfBuildingRepository(fixture.Context);

        var now = DateTime.UtcNow;
        var building = new Building
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Tower One",
            Address = "Rua A",
            City = "Lisbon",
            Country = "PT",
            PostalCode = "1000-001",
            BuildingType = BuildingType.Corporate,
            EstimatedDailyAudience = 100,
            CreatedAt = now,
            UpdatedAt = now
        };
        await repository.AddAsync(building);

        building.Name = "Tower Two";
        building.BuildingType = BuildingType.MixedUse;
        building.UpdatedAt = now.AddMinutes(1);

        var updated = await repository.UpdateAsync(building);

        Assert.NotNull(updated);
        Assert.Equal("Tower Two", updated!.Name);
        Assert.Equal(BuildingType.MixedUse, updated.BuildingType);
    }

    [Fact]
    public async Task Delete_RemovesBuilding()
    {
        using var fixture = new PersistenceTestFixture();
        var repository = new EfBuildingRepository(fixture.Context);

        var now = DateTime.UtcNow;
        var building = new Building
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Tower One",
            Address = "Rua A",
            City = "Lisbon",
            Country = "PT",
            PostalCode = "1000-001",
            BuildingType = BuildingType.Corporate,
            EstimatedDailyAudience = 100,
            CreatedAt = now,
            UpdatedAt = now
        };
        await repository.AddAsync(building);

        var removed = await repository.DeleteAsync(building.Id);
        var retrieved = await repository.GetByIdAsync(building.Id);

        Assert.True(removed);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetAll_ReturnsAllBuildingsOrderedByCreatedAt()
    {
        using var fixture = new PersistenceTestFixture();
        var repository = new EfBuildingRepository(fixture.Context);

        var first = DateTime.UtcNow.AddMinutes(-2);
        await repository.AddAsync(new Building
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "First",
            Address = "A",
            City = "X",
            Country = "Y",
            PostalCode = "0000",
            BuildingType = BuildingType.Corporate,
            EstimatedDailyAudience = 1,
            CreatedAt = first,
            UpdatedAt = first
        });
        await repository.AddAsync(new Building
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Second",
            Address = "A",
            City = "X",
            Country = "Y",
            PostalCode = "0000",
            BuildingType = BuildingType.Corporate,
            EstimatedDailyAudience = 1,
            CreatedAt = first.AddMinutes(1),
            UpdatedAt = first.AddMinutes(1)
        });

        var all = await repository.GetAllAsync();

        Assert.Equal(2, all.Count());
        Assert.Equal("First", all.First().Name);
    }
}

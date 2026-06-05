using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Domain.Entities;

public sealed class InventoryPackage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Cities { get; set; } = [];
    public List<string> BuildingTypes { get; set; } = [];
    public List<string> ScreenOrientations { get; set; } = [];
    public List<Guid> ScreenIds { get; set; } = [];
    public List<Guid> BuildingIds { get; set; } = [];
    public decimal BaseCpm { get; set; }
    public InventoryPackageStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

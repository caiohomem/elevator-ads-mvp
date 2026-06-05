namespace ElevatorAds.Application.DeliveryReports.Dtos;

public sealed record GroupSummaryDto(Guid Id, string Name, int Plays, long PlayedSeconds);

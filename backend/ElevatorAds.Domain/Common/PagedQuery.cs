namespace ElevatorAds.Domain.Common;

public sealed record PagedQuery(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = "asc",
    string? Search = null,
    string? Status = null);

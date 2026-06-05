using System.Net;
using System.Net.Http.Json;
using ElevatorAds.Domain.Common;
using ElevatorAds.Tests.Infrastructure;

namespace ElevatorAds.Tests.Campaigns;

public class BookingRequestEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public BookingRequestEndpointTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PostBookingRequest_CreatesBookingRequest()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var request = new CreateBookingRequestRequest(
            advertiser.Id,
            "Lisbon Summer Launch",
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
            ["Lisbon"],
            ["Corporate"],
            ["Portrait"],
            15,
            500m,
            "Brand awareness",
            "Target premium office buildings");

        var response = await client.PostAsJsonAsync("/api/booking-requests", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var bookingRequest = await response.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(bookingRequest);
        Assert.NotEqual(Guid.Empty, bookingRequest!.Id);
        Assert.Equal(request.AdvertiserId, bookingRequest.AdvertiserId);
        Assert.Equal("Draft", bookingRequest.Status);
    }

    [Fact]
    public async Task GetBookingRequests_ReturnsPagedResult()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);

        await CreateBookingRequestAsync(client, advertiser.Id, "Alpha Request");
        var submitted = await CreateBookingRequestAsync(client, advertiser.Id, "Beta Request");
        await client.PostAsync($"/api/booking-requests/{submitted.Id}/submit", null);

        var response = await client.GetAsync("/api/booking-requests?page=1&pageSize=10&status=Submitted");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<BookingRequestDto>>();
        Assert.NotNull(page);
        Assert.Single(page!.Items);
        Assert.Equal(submitted.Id, page.Items[0].Id);
    }

    [Fact]
    public async Task GetBookingRequest_ById_ReturnsBookingRequest()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var created = await CreateBookingRequestAsync(client, advertiser.Id);

        var response = await client.GetAsync($"/api/booking-requests/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bookingRequest = await response.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(bookingRequest);
        Assert.Equal(created.Id, bookingRequest!.Id);
    }

    [Fact]
    public async Task PutBookingRequest_UpdatesDraftBookingRequest()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var created = await CreateBookingRequestAsync(client, advertiser.Id);
        var request = new UpdateBookingRequestRequest(
            "Updated Booking",
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
            ["Porto"],
            ["Commercial"],
            ["Landscape"],
            10,
            250m,
            "Lead generation",
            "Updated notes");

        var response = await client.PutAsJsonAsync($"/api/booking-requests/{created.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bookingRequest = await response.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(bookingRequest);
        Assert.Equal(request.Name, bookingRequest!.Name);
        Assert.Equal("Commercial", bookingRequest.BuildingTypes[0]);
        Assert.Equal(250m, bookingRequest.Budget);
    }

    [Fact]
    public async Task SubmitBookingRequest_TransitionsToSubmitted()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var created = await CreateBookingRequestAsync(client, advertiser.Id);

        var response = await client.PostAsync($"/api/booking-requests/{created.Id}/submit", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bookingRequest = await response.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(bookingRequest);
        Assert.Equal("Submitted", bookingRequest!.Status);
    }

    [Fact]
    public async Task ApproveBookingRequest_TransitionsToApproved()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var created = await CreateBookingRequestAsync(client, advertiser.Id);
        await client.PostAsync($"/api/booking-requests/{created.Id}/submit", null);

        var response = await client.PostAsync($"/api/booking-requests/{created.Id}/approve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bookingRequest = await response.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(bookingRequest);
        Assert.Equal("Approved", bookingRequest!.Status);
    }

    [Fact]
    public async Task RejectBookingRequest_TransitionsToRejected()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var created = await CreateBookingRequestAsync(client, advertiser.Id);
        await client.PostAsync($"/api/booking-requests/{created.Id}/submit", null);

        var response = await client.PostAsync($"/api/booking-requests/{created.Id}/reject", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bookingRequest = await response.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(bookingRequest);
        Assert.Equal("Rejected", bookingRequest!.Status);
    }

    [Fact]
    public async Task PostBookingRequest_InvalidDateRange_Returns422()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var request = CreateRequest(
            advertiser.Id,
            dateFrom: new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
            dateTo: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));

        var response = await client.PostAsJsonAsync("/api/booking-requests", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostBookingRequest_InvalidCreativeDuration_Returns422()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var request = CreateRequest(advertiser.Id, creativeDurationSeconds: 0);

        var response = await client.PostAsJsonAsync("/api/booking-requests", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostBookingRequest_NegativeBudget_Returns422()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var advertiser = await CreateAdvertiserAsync(client);
        var request = CreateRequest(advertiser.Id, budget: -1m);

        var response = await client.PostAsJsonAsync("/api/booking-requests", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task PostBookingRequest_MissingAdvertiser_Returns422()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();
        var request = CreateRequest(Guid.Empty);

        var response = await client.PostAsJsonAsync("/api/booking-requests", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    private HttpClient CreateClient() => _factory.CreateAuthenticatedClient();

    private static CreateBookingRequestRequest CreateRequest(
        Guid advertiserId,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int creativeDurationSeconds = 15,
        decimal budget = 500m) =>
        new(
            advertiserId,
            "Booking Request",
            dateFrom ?? new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            dateTo ?? new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
            ["Lisbon"],
            ["Corporate"],
            ["Portrait"],
            creativeDurationSeconds,
            budget,
            "Awareness",
            "Notes");

    private async Task<BookingRequestDto> CreateBookingRequestAsync(HttpClient client, Guid advertiserId, string name = "Booking Request")
    {
        var request = CreateRequest(advertiserId) with { Name = name };
        var response = await client.PostAsJsonAsync("/api/booking-requests", request);
        response.EnsureSuccessStatusCode();

        var bookingRequest = await response.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(bookingRequest);
        return bookingRequest!;
    }

    private async Task<AdvertiserDto> CreateAdvertiserAsync(HttpClient client)
    {
        var request = new CreateAdvertiserRequest(
            "Acme",
            "Acme Holdings Ltd",
            "PT123456789",
            "Jane Doe",
            "jane@acme.test",
            "+351123456789",
            "Active");

        var response = await client.PostAsJsonAsync("/api/advertisers", request);
        response.EnsureSuccessStatusCode();

        var advertiser = await response.Content.ReadFromJsonAsync<AdvertiserDto>();
        Assert.NotNull(advertiser);
        return advertiser!;
    }

    private sealed record CreateAdvertiserRequest(
        string Name,
        string LegalName,
        string TaxId,
        string ContactName,
        string ContactEmail,
        string Phone,
        string Status);

    private sealed record AdvertiserDto(Guid Id, string Name);

    private sealed record CreateBookingRequestRequest(
        Guid AdvertiserId,
        string Name,
        DateTime DateFrom,
        DateTime DateTo,
        List<string> Cities,
        List<string> BuildingTypes,
        List<string> ScreenOrientations,
        int CreativeDurationSeconds,
        decimal Budget,
        string CampaignObjective,
        string Notes);

    private sealed record UpdateBookingRequestRequest(
        string Name,
        DateTime DateFrom,
        DateTime DateTo,
        List<string> Cities,
        List<string> BuildingTypes,
        List<string> ScreenOrientations,
        int CreativeDurationSeconds,
        decimal Budget,
        string CampaignObjective,
        string Notes);

    private sealed record BookingRequestDto(
        Guid Id,
        Guid AdvertiserId,
        string Name,
        DateTime DateFrom,
        DateTime DateTo,
        IReadOnlyList<string> Cities,
        IReadOnlyList<string> BuildingTypes,
        IReadOnlyList<string> ScreenOrientations,
        int CreativeDurationSeconds,
        decimal Budget,
        string CampaignObjective,
        string Notes,
        string Status,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}

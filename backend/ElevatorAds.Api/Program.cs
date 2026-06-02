using System.Text.Json.Serialization;
using ElevatorAds.Application.Advertisers;
using ElevatorAds.Application.Advertisers.Dtos;
using ElevatorAds.Application.Buildings;
using ElevatorAds.Application.Buildings.Dtos;
using ElevatorAds.Application.Campaigns;
using ElevatorAds.Application.Creatives;
using ElevatorAds.Application.Creatives.Dtos;
using ElevatorAds.Application.PlaybackReports;
using ElevatorAds.Application.PlaybackReports.Dtos;
using ElevatorAds.Application.Playlists;
using ElevatorAds.Application.Screens;
using ElevatorAds.Application.Screens.Dtos;
using ElevatorAds.Domain.Interfaces;
using ElevatorAds.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);
const string FrontendCorsPolicy = "Frontend";
var configuredCorsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray()
    ?? Array.Empty<string>();

var allowedCorsOrigins = configuredCorsOrigins.Length > 0
    ? configuredCorsOrigins
    : builder.Environment.IsDevelopment()
        ? new[]
        {
            "http://localhost:3000",
            "http://127.0.0.1:3000"
        }
        : Array.Empty<string>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        if (allowedCorsOrigins.Length == 0)
        {
            return;
        }

        policy
            .WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSingleton<IBuildingRepository, InMemoryBuildingRepository>();
builder.Services.AddSingleton<BuildingService>();
builder.Services.AddSingleton<IScreenRepository, InMemoryScreenRepository>();
builder.Services.AddSingleton<ScreenService>();
builder.Services.AddSingleton<IAdvertiserRepository, InMemoryAdvertiserRepository>();
builder.Services.AddSingleton<AdvertiserService>();
builder.Services.AddSingleton<ICreativeRepository, InMemoryCreativeRepository>();
builder.Services.AddSingleton<CreativeService>();
builder.Services.AddSingleton<ICampaignRepository, InMemoryCampaignRepository>();
builder.Services.AddSingleton<CampaignService>();
builder.Services.AddSingleton<ICampaignCreativeRepository, InMemoryCampaignCreativeRepository>();
builder.Services.AddSingleton<CampaignCreativeService>();
builder.Services.AddSingleton<ICampaignDeliveryConstraintsRepository, InMemoryCampaignDeliveryConstraintsRepository>();
builder.Services.AddSingleton<CampaignDeliveryConstraintsService>();
builder.Services.AddSingleton<IDailyPlaylistRepository, InMemoryDailyPlaylistRepository>();
builder.Services.AddSingleton<CampaignEligibilityService>();
builder.Services.AddSingleton<PlaylistGenerationService>();
builder.Services.AddSingleton<PlaylistDownloadService>();
builder.Services.AddSingleton<IProofOfPlayEventRepository, InMemoryProofOfPlayEventRepository>();
builder.Services.AddSingleton<ProofOfPlayService>();

var app = builder.Build();

app.UseCors(FrontendCorsPolicy);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var buildings = app.MapGroup("/api/buildings");

buildings.MapGet("/", async (BuildingService service) => Results.Ok(await service.GetAllAsync()));

buildings.MapGet("/{id:guid}", async (Guid id, BuildingService service) =>
{
    var building = await service.GetByIdAsync(id);
    return building is null ? Results.NotFound() : Results.Ok(building);
});

buildings.MapPost("/", async (CreateBuildingRequest request, BuildingService service) =>
{
    var result = await service.CreateAsync(request);
    return result.IsSuccess
        ? Results.Created($"/api/buildings/{result.Value!.Id}", result.Value)
        : Results.UnprocessableEntity(new { error = result.Error });
});

buildings.MapPut("/{id:guid}", async (Guid id, UpdateBuildingRequest request, BuildingService service) =>
{
    var result = await service.UpdateAsync(id, request);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
});

buildings.MapDelete("/{id:guid}", async (Guid id, BuildingService service) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

var screens = app.MapGroup("/api/screens");

screens.MapGet("/", async (ScreenService service) => Results.Ok(await service.GetAllAsync()));

screens.MapGet("/{id:guid}", async (Guid id, ScreenService service) =>
{
    var screen = await service.GetByIdAsync(id);
    return screen is null ? Results.NotFound() : Results.Ok(screen);
});

screens.MapPost("/", async (CreateScreenRequest request, ScreenService service) =>
{
    var result = await service.CreateAsync(request);
    return result.IsSuccess
        ? Results.Created($"/api/screens/{result.Value!.Id}", result.Value)
        : Results.UnprocessableEntity(new { error = result.Error });
});

screens.MapPut("/{id:guid}", async (Guid id, UpdateScreenRequest request, ScreenService service) =>
{
    var result = await service.UpdateAsync(id, request);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
});

screens.MapDelete("/{id:guid}", async (Guid id, ScreenService service) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

screens.MapPost("/{id:guid}/status-check", async (Guid id, ScreenService service) =>
{
    var screen = await service.StatusCheckAsync(id);
    return screen is null ? Results.NotFound() : Results.Ok(screen);
});

screens.MapGet("/{screenId:guid}/playlists", async (Guid screenId, string? date, IScreenRepository screenRepository, PlaylistGenerationService service) =>
{
    if (await screenRepository.GetByIdAsync(screenId) is null)
    {
        return Results.NotFound();
    }

    DateOnly? parsedDate = null;
    if (date is not null)
    {
        if (!TryParseDate(date, out var screenPlaylistDate))
        {
            return Results.UnprocessableEntity(new { error = "Date is required." });
        }

        parsedDate = screenPlaylistDate;
    }

    var playlists = await service.GetByScreenIdAsync(screenId, parsedDate);
    return Results.Ok(playlists);
});

screens.MapGet("/{screenId:guid}/playlist/current", async (Guid screenId, PlaylistDownloadService service) =>
{
    var playlist = await service.GetCurrentAsync(screenId);
    return playlist is null ? Results.NotFound() : Results.Ok(playlist);
});

screens.MapGet("/{screenId:guid}/playlist", async (Guid screenId, string? date, PlaylistDownloadService service) =>
{
    if (!TryParseDate(date, out var parsedDate))
    {
        return Results.UnprocessableEntity(new { error = "Date is required." });
    }

    var playlist = await service.GetByDateAsync(screenId, parsedDate);
    return playlist is null ? Results.NotFound() : Results.Ok(playlist);
});

screens.MapPost("/{screenId:guid}/playlist/{playlistId:guid}/downloaded", async (Guid screenId, Guid playlistId, PlaylistDownloadService service) =>
{
    var result = await service.MarkDownloadedAsync(screenId, playlistId);
    if (result.IsSuccess)
    {
        return Results.Ok(result.Value);
    }

    return result.WasFound
        ? Results.UnprocessableEntity(new { error = result.Error })
        : Results.NotFound();
});

screens.MapPost("/{screenId:guid}/playback-reports", async (Guid screenId, CreatePlaybackReportRequest request, ProofOfPlayService service) =>
{
    var result = await service.CreateAsync(screenId, request);
    if (result.IsSuccess)
    {
        return Results.Created($"/api/screens/{screenId}/playback-reports", result.Value);
    }

    return result.WasFound
        ? Results.UnprocessableEntity(new { error = result.Error })
        : Results.NotFound();
});

screens.MapGet("/{screenId:guid}/playback-reports", async (Guid screenId, ProofOfPlayService service) =>
    Results.Ok(await service.GetByScreenAsync(screenId)));

var advertisers = app.MapGroup("/api/advertisers");

advertisers.MapGet("/", async (AdvertiserService service) => Results.Ok(await service.GetAllAsync()));

advertisers.MapGet("/{id:guid}", async (Guid id, AdvertiserService service) =>
{
    var advertiser = await service.GetByIdAsync(id);
    return advertiser is null ? Results.NotFound() : Results.Ok(advertiser);
});

advertisers.MapPost("/", async (CreateAdvertiserRequest request, AdvertiserService service) =>
{
    var result = await service.CreateAsync(request);
    return result.IsSuccess
        ? Results.Created($"/api/advertisers/{result.Value!.Id}", result.Value)
        : Results.UnprocessableEntity(new { error = result.Error });
});

advertisers.MapPut("/{id:guid}", async (Guid id, UpdateAdvertiserRequest request, AdvertiserService service) =>
{
    var result = await service.UpdateAsync(id, request);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
});

advertisers.MapDelete("/{id:guid}", async (Guid id, AdvertiserService service) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

var creatives = app.MapGroup("/api/creatives");

creatives.MapGet("/", async (CreativeService service) => Results.Ok(await service.GetAllAsync()));

creatives.MapGet("/{id:guid}", async (Guid id, CreativeService service) =>
{
    var creative = await service.GetByIdAsync(id);
    return creative is null ? Results.NotFound() : Results.Ok(creative);
});

creatives.MapPost("/", async (CreateCreativeRequest request, CreativeService service) =>
{
    var result = await service.CreateAsync(request);
    return result.IsSuccess
        ? Results.Created($"/api/creatives/{result.Value!.Id}", result.Value)
        : Results.UnprocessableEntity(new { error = result.Error });
});

creatives.MapPut("/{id:guid}", async (Guid id, UpdateCreativeRequest request, CreativeService service) =>
{
    var result = await service.UpdateAsync(id, request);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
});

creatives.MapDelete("/{id:guid}", async (Guid id, CreativeService service) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

creatives.MapPost("/{id:guid}/submit-for-review", async (Guid id, CreativeService service) =>
{
    var result = await service.SubmitForReviewAsync(id);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
});

creatives.MapPost("/{id:guid}/approve", async (Guid id, CreativeService service) =>
{
    var result = await service.ApproveAsync(id);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
});

creatives.MapPost("/{id:guid}/reject", async (Guid id, CreativeService service) =>
{
    var result = await service.RejectAsync(id);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
});

var campaigns = app.MapGroup("/api/campaigns");

campaigns.MapGet("/", async (CampaignService service) => Results.Ok(await service.GetAllAsync()));

campaigns.MapGet("/{id:guid}", async (Guid id, CampaignService service) =>
{
    var campaign = await service.GetByIdAsync(id);
    return campaign is null ? Results.NotFound() : Results.Ok(campaign);
});

campaigns.MapPost("/", async (CampaignService.CreateCampaignRequest request, CampaignService service) =>
{
    var result = await service.CreateAsync(request);
    return result.IsSuccess
        ? Results.Created($"/api/campaigns/{result.Value!.Id}", result.Value)
        : Results.UnprocessableEntity(new { error = result.Error });
});

campaigns.MapPut("/{id:guid}", async (Guid id, CampaignService.UpdateCampaignRequest request, CampaignService service) =>
{
    var result = await service.UpdateAsync(id, request);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
});

campaigns.MapDelete("/{id:guid}", async (Guid id, CampaignService service) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

campaigns.MapGet("/{campaignId:guid}/creatives", async (Guid campaignId, CampaignCreativeService service) =>
{
    var result = await service.GetByCampaignIdAsync(campaignId);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
});

campaigns.MapPost("/{campaignId:guid}/creatives/{creativeId:guid}", async (Guid campaignId, Guid creativeId, CampaignCreativeService service) =>
{
    var result = await service.AssignAsync(campaignId, creativeId);
    return result.IsSuccess
        ? Results.Created($"/api/campaigns/{campaignId}/creatives/{creativeId}", result.Value)
        : Results.UnprocessableEntity(new { error = result.Error });
});

campaigns.MapDelete("/{campaignId:guid}/creatives/{creativeId:guid}", async (Guid campaignId, Guid creativeId, CampaignCreativeService service) =>
    await service.RemoveAsync(campaignId, creativeId) ? Results.NoContent() : Results.NotFound());

campaigns.MapGet("/{campaignId:guid}/delivery-constraints", async (Guid campaignId, CampaignDeliveryConstraintsService service) =>
{
    var constraints = await service.GetByCampaignIdAsync(campaignId);
    return constraints is null ? Results.NotFound() : Results.Ok(constraints);
});

campaigns.MapPut(
    "/{campaignId:guid}/delivery-constraints",
    async (Guid campaignId, CampaignDeliveryConstraintsService.UpsertDeliveryConstraintsRequest request, CampaignDeliveryConstraintsService service) =>
    {
        var result = await service.UpsertAsync(campaignId, request);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.UnprocessableEntity(new { error = result.Error });
    });

campaigns.MapGet("/{campaignId:guid}/playback-reports", async (Guid campaignId, ProofOfPlayService service) =>
    Results.Ok(await service.GetByCampaignAsync(campaignId)));

var playlists = app.MapGroup("/api/playlists");

playlists.MapPost("/generate", async (string? date, PlaylistGenerationService service) =>
{
    if (!TryParseDate(date, out var parsedDate))
    {
        return Results.UnprocessableEntity(new { error = "Date is required." });
    }

    var generated = await service.GenerateAsync(parsedDate);
    return Results.Ok(generated);
});

playlists.MapGet("/", async (PlaylistGenerationService service) => Results.Ok(await service.GetAllAsync()));

playlists.MapGet("/{id:guid}", async (Guid id, PlaylistGenerationService service) =>
{
    var playlist = await service.GetByIdAsync(id);
    return playlist is null ? Results.NotFound() : Results.Ok(playlist);
});

playlists.MapPost("/{id:guid}/publish", async (Guid id, PlaylistGenerationService service) =>
{
    var published = await service.PublishAsync(id);
    return published is null
        ? Results.UnprocessableEntity(new { error = "Playlist not found or cannot be published." })
        : Results.Ok(published);
});

var playbackReports = app.MapGroup("/api/playback-reports");

playbackReports.MapGet("/", async (ProofOfPlayService service) => Results.Ok(await service.GetAllAsync()));

static bool TryParseDate(string? value, out DateOnly date) =>
    DateOnly.TryParseExact(value, "yyyy-MM-dd", out date);

app.Run();

public partial class Program;

using System.Text.Json.Serialization;
using ElevatorAds.Application.Advertisers;
using ElevatorAds.Application.Advertisers.Dtos;
using ElevatorAds.Application.Buildings;
using ElevatorAds.Application.Buildings.Dtos;
using ElevatorAds.Application.Campaigns;
using ElevatorAds.Application.Creatives;
using ElevatorAds.Application.Creatives.Dtos;
using ElevatorAds.Application.Screens;
using ElevatorAds.Application.Screens.Dtos;
using ElevatorAds.Domain.Interfaces;
using ElevatorAds.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

var app = builder.Build();

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

app.Run();

public partial class Program;

using System.Text.Json.Serialization;
using ElevatorAds.Application.Buildings;
using ElevatorAds.Application.Buildings.Dtos;
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

app.Run();

public partial class Program;

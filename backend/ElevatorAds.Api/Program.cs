using System.Text.Json.Serialization;
using ElevatorAds.Application.Buildings;
using ElevatorAds.Application.Buildings.Dtos;
using ElevatorAds.Domain.Interfaces;
using ElevatorAds.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton<IBuildingRepository, InMemoryBuildingRepository>();
builder.Services.AddSingleton<BuildingService>();

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

app.Run();

public partial class Program;

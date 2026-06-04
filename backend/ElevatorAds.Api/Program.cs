using System.Text;
using System.Text.Json.Serialization;
using ElevatorAds.Api.Auth;
using ElevatorAds.Application.Advertisers;
using ElevatorAds.Application.Advertisers.Dtos;
using ElevatorAds.Application.Auth;
using ElevatorAds.Domain.Common;
using ElevatorAds.Application.Buildings;
using ElevatorAds.Application.Buildings.Dtos;
using ElevatorAds.Application.Campaigns;
using ElevatorAds.Application.Creatives;
using ElevatorAds.Application.Creatives.Dtos;
using ElevatorAds.Application.DeliveryReports;
using ElevatorAds.Application.Organizations;
using ElevatorAds.Application.Organizations.Dtos;
using ElevatorAds.Application.PlaybackReports;
using ElevatorAds.Application.PlaybackReports.Dtos;
using ElevatorAds.Application.Playlists;
using ElevatorAds.Application.Screens;
using ElevatorAds.Application.Screens.Dtos;
using ElevatorAds.Domain.Interfaces;
using ElevatorAds.Infrastructure.Persistence;
using ElevatorAds.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<IBuildingRepository, EfBuildingRepository>();
builder.Services.AddScoped<BuildingService>();
builder.Services.AddScoped<IScreenRepository, EfScreenRepository>();
builder.Services.AddScoped<ScreenService>();
builder.Services.AddScoped<IAdvertiserRepository, EfAdvertiserRepository>();
builder.Services.AddScoped<AdvertiserService>();
builder.Services.AddScoped<ICreativeRepository, EfCreativeRepository>();
builder.Services.AddScoped<CreativeService>();
builder.Services.AddScoped<ICampaignRepository, EfCampaignRepository>();
builder.Services.AddScoped<CampaignService>();
builder.Services.AddScoped<ICampaignCreativeRepository, EfCampaignCreativeRepository>();
builder.Services.AddScoped<CampaignCreativeService>();
builder.Services.AddScoped<ICampaignDeliveryConstraintsRepository, EfCampaignDeliveryConstraintsRepository>();
builder.Services.AddScoped<CampaignDeliveryConstraintsService>();
builder.Services.AddScoped<IDailyPlaylistRepository, EfDailyPlaylistRepository>();
builder.Services.AddScoped<CampaignEligibilityService>();
builder.Services.AddScoped<PlaylistGenerationService>();
builder.Services.AddScoped<PlaylistDownloadService>();
builder.Services.AddScoped<IProofOfPlayEventRepository, EfProofOfPlayEventRepository>();
builder.Services.AddScoped<ProofOfPlayService>();
builder.Services.AddScoped<DeliveryReportService>();
builder.Services.AddScoped<IOrganizationRepository, EfOrganizationRepository>();
builder.Services.AddScoped<OrganizationService>();

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ElevatorAds";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ElevatorAds.Clients";
var jwtSecret = Environment.GetEnvironmentVariable("JWT__Secret")
    ?? builder.Configuration["JWT:Secret"];

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (builder.Environment.IsDevelopment())
    {
        jwtSecret = "development-only-secret-please-override-32-bytes-minimum";
    }
    else
    {
        throw new InvalidOperationException("JWT secret is not configured. Set the JWT__Secret environment variable.");
    }
}

if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException("JWT secret must be at least 32 bytes for HS256 signing.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = AuthService.UsernameClaim,
            RoleClaimType = AuthService.RoleClaim
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.AdminPolicy, policy =>
        policy.RequireAuthenticatedUser().RequireClaim(AuthService.RoleClaim, AuthPolicies.AdminRole));

    options.AddPolicy(AuthPolicies.OperatorPolicy, policy =>
        policy.RequireAuthenticatedUser().RequireClaim(
            AuthService.RoleClaim,
            AuthPolicies.AdminRole,
            AuthPolicies.OperatorRole));

    options.AddPolicy(AuthPolicies.ViewerPolicy, policy =>
        policy.RequireAuthenticatedUser());
});

var app = builder.Build();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
    await DatabaseSeeder.SeedAdminUserAsync(scope.ServiceProvider, logger);
    await DatabaseSeeder.SeedDefaultOrganizationAsync(scope.ServiceProvider, logger);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var auth = app.MapGroup("/api/auth");

auth.MapPost("/login", async (LoginRequest request, AuthService service, CancellationToken cancellationToken) =>
{
    var outcome = await service.ValidateAndLoginAsync(request, cancellationToken);
    return outcome.IsSuccess
        ? Results.Ok(outcome.Response)
        : Results.Json(new { error = outcome.Error }, statusCode: StatusCodes.Status401Unauthorized);
});

var organizations = app.MapGroup("/api/organizations");

organizations.MapGet("/", async ([AsParameters] PagedQuery query, OrganizationService service) =>
{
    if (ValidatePagedQuery(query) is { } error)
    {
        return Results.BadRequest(new { error });
    }

    return Results.Ok(await service.GetPagedAsync(query));
}).RequireAuthorization(AuthPolicies.ViewerPolicy);

organizations.MapGet("/{id:guid}", async (Guid id, OrganizationService service) =>
{
    var org = await service.GetByIdAsync(id);
    return org is null ? Results.NotFound() : Results.Ok(org);
}).RequireAuthorization(AuthPolicies.ViewerPolicy);

organizations.MapGet("/by-slug/{slug}", async (string slug, OrganizationService service) =>
{
    var org = await service.GetBySlugAsync(slug);
    return org is null ? Results.NotFound() : Results.Ok(org);
}).RequireAuthorization(AuthPolicies.ViewerPolicy);

organizations.MapPost("/", async (CreateOrganizationRequest request, OrganizationService service) =>
{
    var result = await service.CreateAsync(request);
    return result.IsSuccess
        ? Results.Created($"/api/organizations/{result.Value!.Id}", result.Value)
        : Results.UnprocessableEntity(new { error = result.Error });
}).RequireAuthorization(AuthPolicies.AdminPolicy);

organizations.MapPut("/{id:guid}", async (Guid id, UpdateOrganizationRequest request, OrganizationService service) =>
{
    var result = await service.UpdateAsync(id, request);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
}).RequireAuthorization(AuthPolicies.AdminPolicy);

organizations.MapDelete("/{id:guid}", async (Guid id, OrganizationService service) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound())
    .RequireAuthorization(AuthPolicies.AdminPolicy);

var buildings = app.MapGroup("/api/buildings");

buildings.MapGet("/", async ([AsParameters] PagedQuery query, BuildingService service) =>
{
    if (ValidatePagedQuery(query) is { } error)
    {
        return Results.BadRequest(new { error });
    }

    return Results.Ok(await service.GetPagedAsync(query));
});

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
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

buildings.MapPut("/{id:guid}", async (Guid id, UpdateBuildingRequest request, BuildingService service) =>
{
    var result = await service.UpdateAsync(id, request);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

buildings.MapDelete("/{id:guid}", async (Guid id, BuildingService service) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound())
    .RequireAuthorization(AuthPolicies.OperatorPolicy);

var screens = app.MapGroup("/api/screens");

screens.MapGet("/", async ([AsParameters] PagedQuery query, ScreenService service) =>
{
    if (ValidatePagedQuery(query) is { } error)
    {
        return Results.BadRequest(new { error });
    }

    return Results.Ok(await service.GetPagedAsync(query));
});

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
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

screens.MapPut("/{id:guid}", async (Guid id, UpdateScreenRequest request, ScreenService service) =>
{
    var result = await service.UpdateAsync(id, request);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

screens.MapDelete("/{id:guid}", async (Guid id, ScreenService service) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound())
    .RequireAuthorization(AuthPolicies.OperatorPolicy);

screens.MapPost("/{id:guid}/status-check", async (Guid id, ScreenService service) =>
{
    var screen = await service.StatusCheckAsync(id);
    return screen is null ? Results.NotFound() : Results.Ok(screen);
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

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
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

// Playback-report POST is intentionally left public: this is a device-facing
// endpoint called by screens/players to report proof-of-play. Screens do not
// authenticate in the MVP; access control here is enforced via network/edge
// controls in the deployment environment.
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

screens.MapGet("/{screenId:guid}/playback-reports", async (Guid screenId, [AsParameters] PagedQuery query, ProofOfPlayService service) =>
{
    if (ValidatePagedQuery(query) is { } error)
    {
        return Results.BadRequest(new { error });
    }

    return Results.Ok(await service.GetPagedByScreenAsync(screenId, query));
});

var advertisers = app.MapGroup("/api/advertisers");

advertisers.MapGet("/", async ([AsParameters] PagedQuery query, AdvertiserService service) =>
{
    if (ValidatePagedQuery(query) is { } error)
    {
        return Results.BadRequest(new { error });
    }

    return Results.Ok(await service.GetPagedAsync(query));
});

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
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

advertisers.MapPut("/{id:guid}", async (Guid id, UpdateAdvertiserRequest request, AdvertiserService service) =>
{
    var result = await service.UpdateAsync(id, request);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

advertisers.MapDelete("/{id:guid}", async (Guid id, AdvertiserService service) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound())
    .RequireAuthorization(AuthPolicies.OperatorPolicy);

var creatives = app.MapGroup("/api/creatives");

creatives.MapGet("/", async ([AsParameters] PagedQuery query, CreativeService service) =>
{
    if (ValidatePagedQuery(query) is { } error)
    {
        return Results.BadRequest(new { error });
    }

    return Results.Ok(await service.GetPagedAsync(query));
});

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
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

creatives.MapPut("/{id:guid}", async (Guid id, UpdateCreativeRequest request, CreativeService service) =>
{
    var result = await service.UpdateAsync(id, request);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

creatives.MapDelete("/{id:guid}", async (Guid id, CreativeService service) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound())
    .RequireAuthorization(AuthPolicies.OperatorPolicy);

creatives.MapPost("/{id:guid}/submit-for-review", async (Guid id, CreativeService service) =>
{
    var result = await service.SubmitForReviewAsync(id);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

creatives.MapPost("/{id:guid}/approve", async (Guid id, CreativeService service) =>
{
    var result = await service.ApproveAsync(id);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
}).RequireAuthorization(AuthPolicies.AdminPolicy);

creatives.MapPost("/{id:guid}/reject", async (Guid id, CreativeService service) =>
{
    var result = await service.RejectAsync(id);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
}).RequireAuthorization(AuthPolicies.AdminPolicy);

var campaigns = app.MapGroup("/api/campaigns");

campaigns.MapGet("/", async ([AsParameters] PagedQuery query, CampaignService service) =>
{
    if (ValidatePagedQuery(query) is { } error)
    {
        return Results.BadRequest(new { error });
    }

    return Results.Ok(await service.GetPagedAsync(query));
});

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
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

campaigns.MapPut("/{id:guid}", async (Guid id, CampaignService.UpdateCampaignRequest request, CampaignService service) =>
{
    var result = await service.UpdateAsync(id, request);
    if (!result.IsSuccess)
    {
        return Results.UnprocessableEntity(new { error = result.Error });
    }

    return result.Value is null ? Results.NotFound() : Results.Ok(result.Value);
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

campaigns.MapDelete("/{id:guid}", async (Guid id, CampaignService service) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound())
    .RequireAuthorization(AuthPolicies.OperatorPolicy);

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
}).RequireAuthorization(AuthPolicies.OperatorPolicy);

campaigns.MapDelete("/{campaignId:guid}/creatives/{creativeId:guid}", async (Guid campaignId, Guid creativeId, CampaignCreativeService service) =>
    await service.RemoveAsync(campaignId, creativeId) ? Results.NoContent() : Results.NotFound())
    .RequireAuthorization(AuthPolicies.OperatorPolicy);

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
    }).RequireAuthorization(AuthPolicies.OperatorPolicy);

campaigns.MapGet("/{campaignId:guid}/playback-reports", async (Guid campaignId, [AsParameters] PagedQuery query, ProofOfPlayService service) =>
{
    if (ValidatePagedQuery(query) is { } error)
    {
        return Results.BadRequest(new { error });
    }

    return Results.Ok(await service.GetPagedByCampaignAsync(campaignId, query));
});

var playlists = app.MapGroup("/api/playlists");

playlists.MapPost("/generate", async (string? date, PlaylistGenerationService service) =>
{
    if (!TryParseDate(date, out var parsedDate))
    {
        return Results.UnprocessableEntity(new { error = "Date is required." });
    }

    var generated = await service.GenerateAsync(parsedDate);
    return Results.Ok(generated);
}).RequireAuthorization(AuthPolicies.AdminPolicy);

playlists.MapGet("/", async ([AsParameters] PagedQuery query, PlaylistGenerationService service) =>
{
    if (ValidatePagedQuery(query) is { } error)
    {
        return Results.BadRequest(new { error });
    }

    return Results.Ok(await service.GetPagedAsync(query));
});

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
}).RequireAuthorization(AuthPolicies.AdminPolicy);

var playbackReports = app.MapGroup("/api/playback-reports");

playbackReports.MapGet("/", async ([AsParameters] PagedQuery query, ProofOfPlayService service) =>
{
    if (ValidatePagedQuery(query) is { } error)
    {
        return Results.BadRequest(new { error });
    }

    return Results.Ok(await service.GetPagedAsync(query));
});

var reports = app.MapGroup("/api/reports");

reports.MapGet("/overview", async (string? date, DeliveryReportService service) =>
{
    if (!TryParseDate(date, out var parsedDate))
    {
        return Results.UnprocessableEntity(new { error = "Date is required." });
    }

    return Results.Ok(await service.GetOverviewAsync(parsedDate));
});

reports.MapGet("/campaigns", async (string? from, string? to, DeliveryReportService service) =>
{
    if (!TryParseDate(from, out var fromDate) || !TryParseDate(to, out var toDate))
    {
        return Results.UnprocessableEntity(new { error = "From and To dates are required." });
    }

    if (toDate < fromDate)
    {
        return Results.UnprocessableEntity(new { error = "To must be greater than or equal to From." });
    }

    return Results.Ok(await service.GetCampaignsAsync(fromDate, toDate));
});

reports.MapGet("/screens", async (string? from, string? to, DeliveryReportService service) =>
{
    if (!TryParseDate(from, out var fromDate) || !TryParseDate(to, out var toDate))
    {
        return Results.UnprocessableEntity(new { error = "From and To dates are required." });
    }

    if (toDate < fromDate)
    {
        return Results.UnprocessableEntity(new { error = "To must be greater than or equal to From." });
    }

    return Results.Ok(await service.GetScreensAsync(fromDate, toDate));
});

static bool TryParseDate(string? value, out DateOnly date) =>
    DateOnly.TryParseExact(value, "yyyy-MM-dd", out date);

static string? ValidatePagedQuery(PagedQuery query)
{
    if (query.Page < 1)
    {
        return "page must be greater than 0.";
    }

    if (query.PageSize is < 1 or > 100)
    {
        return "pageSize must be between 1 and 100.";
    }

    return null;
}

app.Run();

public partial class Program;

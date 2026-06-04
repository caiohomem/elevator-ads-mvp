using System.Net.Http.Headers;
using ElevatorAds.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ElevatorAds.Tests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestJwtSecret = "test-only-secret-must-be-at-least-32-bytes-long!!";

    static TestWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("JWT__Secret", TestJwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestTokenIssuer.Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestTokenIssuer.Audience);
        Environment.SetEnvironmentVariable("Jwt__LifetimeMinutes", "30");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var databaseName = $"ElevatorAds-Tests-{Guid.NewGuid():N}";

        builder.UseEnvironment("Development");

        Environment.SetEnvironmentVariable("JWT__Secret", TestJwtSecret);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = TestTokenIssuer.Issuer,
                ["Jwt:Audience"] = TestTokenIssuer.Audience,
                ["Jwt:LifetimeMinutes"] = "30",
                ["JWT:Secret"] = TestJwtSecret
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptionsBuilder<AppDbContext>>();

            var providerConfigurations = services
                .Where(descriptor => descriptor.ServiceType.IsGenericType
                    && descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>))
                .ToList();
            foreach (var descriptor in providerConfigurations)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }

    public HttpClient CreateAuthenticatedClient(string? token = null)
    {
        var client = CreateClient();
        var bearer = token ?? TestTokenIssuer.IssueOperatorToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return client;
    }
}

using ElevatorAds.Application.Advertisers;

namespace ElevatorAds.Api.Auth;

public static class AdvertiserApiKeyEndpointExtensions
{
    public static TBuilder RequireAdvertiserApiKeyScope<TBuilder>(this TBuilder builder, string requiredScope)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            var apiKeyService = httpContext.RequestServices.GetRequiredService<AdvertiserApiKeyService>();
            var rawApiKey = httpContext.Request.Headers["X-Api-Key"].FirstOrDefault();
            var validation = await apiKeyService.ValidateAsync(rawApiKey, requiredScope);

            if (!validation.IsValid)
            {
                return Results.Json(new { error = validation.Error }, statusCode: StatusCodes.Status401Unauthorized);
            }

            httpContext.Items[AdvertiserApiKeyHttpContext.ApiKeyIdKey] = validation.ApiKeyId;
            httpContext.Items[AdvertiserApiKeyHttpContext.AdvertiserIdKey] = validation.AdvertiserId;
            httpContext.Items[AdvertiserApiKeyHttpContext.KeyPrefixKey] = validation.KeyPrefix;
            httpContext.Items[AdvertiserApiKeyHttpContext.ScopesKey] = validation.Scopes;

            return await next(context);
        });

        return builder;
    }
}

public static class AdvertiserApiKeyHttpContext
{
    public const string ApiKeyIdKey = "AdvertiserApiKey.ApiKeyId";
    public const string AdvertiserIdKey = "AdvertiserApiKey.AdvertiserId";
    public const string KeyPrefixKey = "AdvertiserApiKey.KeyPrefix";
    public const string ScopesKey = "AdvertiserApiKey.Scopes";
}

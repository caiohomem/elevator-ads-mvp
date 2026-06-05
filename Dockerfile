FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/ElevatorAds.sln backend/
COPY backend/ElevatorAds.Api/ElevatorAds.Api.csproj        backend/ElevatorAds.Api/
COPY backend/ElevatorAds.Application/ElevatorAds.Application.csproj backend/ElevatorAds.Application/
COPY backend/ElevatorAds.Domain/ElevatorAds.Domain.csproj    backend/ElevatorAds.Domain/
COPY backend/ElevatorAds.Infrastructure/ElevatorAds.Infrastructure.csproj backend/ElevatorAds.Infrastructure/
RUN dotnet restore backend/ElevatorAds.Api/ElevatorAds.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/ElevatorAds.Api/ElevatorAds.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ElevatorAds.Api.dll"]

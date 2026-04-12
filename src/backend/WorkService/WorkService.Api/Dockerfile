FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY WorkService.Domain/WorkService.Domain.csproj WorkService.Domain/
COPY WorkService.Application/WorkService.Application.csproj WorkService.Application/
COPY WorkService.Infrastructure/WorkService.Infrastructure.csproj WorkService.Infrastructure/
COPY WorkService.Api/WorkService.Api.csproj WorkService.Api/

# Restore
RUN dotnet restore WorkService.Api/WorkService.Api.csproj

# Copy source
COPY . .

# Build
WORKDIR /src/WorkService.Api
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5003
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:5003/health || exit 1
ENTRYPOINT ["dotnet", "WorkService.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY SecurityService.Domain/SecurityService.Domain.csproj SecurityService.Domain/
COPY SecurityService.Application/SecurityService.Application.csproj SecurityService.Application/
COPY SecurityService.Infrastructure/SecurityService.Infrastructure.csproj SecurityService.Infrastructure/
COPY SecurityService.Api/SecurityService.Api.csproj SecurityService.Api/

# Restore
RUN dotnet restore SecurityService.Api/SecurityService.Api.csproj

# Copy source
COPY . .

# Build
WORKDIR /src/SecurityService.Api
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5001
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:5001/health || exit 1
ENTRYPOINT ["dotnet", "SecurityService.Api.dll"]

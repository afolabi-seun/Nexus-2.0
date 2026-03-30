FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5200

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY UtilityService.Domain/UtilityService.Domain.csproj UtilityService.Domain/
COPY UtilityService.Application/UtilityService.Application.csproj UtilityService.Application/
COPY UtilityService.Infrastructure/UtilityService.Infrastructure.csproj UtilityService.Infrastructure/
COPY UtilityService.Api/UtilityService.Api.csproj UtilityService.Api/

# Restore
RUN dotnet restore UtilityService.Api/UtilityService.Api.csproj

# Copy source
COPY . .

# Build
WORKDIR /src/UtilityService.Api
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5200
ENTRYPOINT ["dotnet", "UtilityService.Api.dll"]

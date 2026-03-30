FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ProfileService.Domain/ProfileService.Domain.csproj ProfileService.Domain/
COPY ProfileService.Application/ProfileService.Application.csproj ProfileService.Application/
COPY ProfileService.Infrastructure/ProfileService.Infrastructure.csproj ProfileService.Infrastructure/
COPY ProfileService.Api/ProfileService.Api.csproj ProfileService.Api/

# Restore
RUN dotnet restore ProfileService.Api/ProfileService.Api.csproj

# Copy source
COPY . .

# Build
WORKDIR /src/ProfileService.Api
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5002
ENTRYPOINT ["dotnet", "ProfileService.Api.dll"]

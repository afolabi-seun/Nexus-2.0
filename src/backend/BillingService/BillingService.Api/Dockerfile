FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5300

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY BillingService.Domain/BillingService.Domain.csproj BillingService.Domain/
COPY BillingService.Application/BillingService.Application.csproj BillingService.Application/
COPY BillingService.Infrastructure/BillingService.Infrastructure.csproj BillingService.Infrastructure/
COPY BillingService.Api/BillingService.Api.csproj BillingService.Api/

# Restore
RUN dotnet restore BillingService.Api/BillingService.Api.csproj

# Copy source
COPY . .

# Build
WORKDIR /src/BillingService.Api
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5300
ENTRYPOINT ["dotnet", "BillingService.Api.dll"]

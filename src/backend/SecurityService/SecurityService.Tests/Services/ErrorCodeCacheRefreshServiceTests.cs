using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityService.Domain.Interfaces.Services.ErrorCodeResolver;
using SecurityService.Infrastructure.Services.BackgroundServices;

namespace SecurityService.Tests.Services;

/// <summary>
/// Unit tests for ErrorCodeCacheRefreshService.
/// Validates: Requirements 5.2, 5.4, 5.6
/// </summary>
public class ErrorCodeCacheRefreshServiceTests
{
    private readonly Mock<IErrorCodeResolverService> _resolverMock;
    private readonly Mock<ILogger<ErrorCodeCacheRefreshService>> _loggerMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;

    public ErrorCodeCacheRefreshServiceTests()
    {
        _resolverMock = new Mock<IErrorCodeResolverService>();
        _loggerMock = new Mock<ILogger<ErrorCodeCacheRefreshService>>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();

        var scopeMock = new Mock<IServiceScope>();
        var providerMock = new Mock<IServiceProvider>();

        providerMock
            .Setup(p => p.GetService(typeof(IErrorCodeResolverService)))
            .Returns(_resolverMock.Object);

        scopeMock.Setup(s => s.ServiceProvider).Returns(providerMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
    }

    /// <summary>
    /// Validates: Requirement 5.2 — initial cache refresh on startup.
    /// </summary>
    [Fact]
    public async Task StartAsync_CallsRefreshCacheAsync_OnStartup()
    {
        // Arrange
        var service = new ErrorCodeCacheRefreshService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource();

        _resolverMock
            .Setup(r => r.RefreshCacheAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act — start the service, give it a moment to execute, then cancel
        var executeTask = StartServiceAsync(service, cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        // Allow the service to finish gracefully (Task.Delay throws on cancellation)
        try { await executeTask; } catch (OperationCanceledException) { }

        // Assert
        _resolverMock.Verify(
            r => r.RefreshCacheAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Validates: Requirement 5.4 — failure during refresh logs warning and does not crash.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenRefreshThrows_LogsWarningAndDoesNotCrash()
    {
        // Arrange
        _resolverMock
            .Setup(r => r.RefreshCacheAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("UtilityService unavailable"));

        var service = new ErrorCodeCacheRefreshService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource();

        // Act — start the service, let it hit the exception, then cancel
        var executeTask = StartServiceAsync(service, cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        // The service should NOT throw — it catches and logs
        try { await executeTask; } catch (OperationCanceledException) { }

        // Assert — verify warning was logged
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Validates: Requirement 5.6 — coexistence with ErrorCodeValidationHostedService.
    /// Both services can be registered and resolved from the DI container without conflict.
    /// </summary>
    [Fact]
    public void BothHostedServices_CanBeRegistered_WithoutConflict()
    {
        // Arrange
        var services = new ServiceCollection();

        // Register dependencies needed by both services
        services.AddSingleton(_scopeFactoryMock.Object);
        services.AddSingleton<IHttpClientFactory>(new Mock<IHttpClientFactory>().Object);
        services.AddLogging();

        // Register both hosted services — same as production DI
        services.AddHostedService<ErrorCodeValidationHostedService>();
        services.AddHostedService<ErrorCodeCacheRefreshService>();

        // Act
        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();

        // Assert — both services are present and distinct
        Assert.Contains(hostedServices, s => s is ErrorCodeValidationHostedService);
        Assert.Contains(hostedServices, s => s is ErrorCodeCacheRefreshService);
        Assert.True(hostedServices.Count(s =>
            s is ErrorCodeValidationHostedService || s is ErrorCodeCacheRefreshService) >= 2);
    }

    /// <summary>
    /// Helper to invoke the protected ExecuteAsync via StartAsync/StopAsync.
    /// </summary>
    private static async Task StartServiceAsync(BackgroundService service, CancellationToken ct)
    {
        await service.StartAsync(ct);
        // BackgroundService.StartAsync fires ExecuteAsync in the background.
        // We need to await the ExecuteTask to observe exceptions.
        await (service.ExecuteTask ?? Task.CompletedTask);
    }
}

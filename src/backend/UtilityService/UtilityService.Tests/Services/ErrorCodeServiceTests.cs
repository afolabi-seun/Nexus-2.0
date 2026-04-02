using Microsoft.EntityFrameworkCore;
using Moq;
using StackExchange.Redis;
using UtilityService.Application.DTOs.ErrorCodes;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Exceptions;
using UtilityService.Domain.Interfaces.Repositories.ErrorCodeEntries;
using UtilityService.Infrastructure.Data;
using UtilityService.Infrastructure.Services.ErrorCodes;

namespace UtilityService.Tests.Services;

public class ErrorCodeServiceTests
{
    private readonly Mock<IErrorCodeEntryRepository> _repoMock = new();
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _dbMock = new();
    private readonly ErrorCodeService _sut;

    public ErrorCodeServiceTests()
    {
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        _dbMock.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        var dbContext = new UtilityDbContext(new DbContextOptionsBuilder<UtilityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _sut = new ErrorCodeService(_repoMock.Object, _redisMock.Object, dbContext);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsResponse()
    {
        var request = new CreateErrorCodeRequest
        {
            Code = "TEST_001", Value = 1001, HttpStatusCode = 400,
            ResponseCode = "01", Description = "Test error", ServiceName = "TestSvc"
        };
        _repoMock.Setup(r => r.GetByCodeAsync("TEST_001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorCodeEntry?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<ErrorCodeEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorCodeEntry e, CancellationToken _) => e);

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result);
        var response = Assert.IsType<ErrorCodeResponse>(result);
        Assert.Equal("TEST_001", response.Code);
        Assert.Equal(1001, response.Value);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsErrorCodeDuplicateException()
    {
        var request = new CreateErrorCodeRequest { Code = "DUP_CODE", Value = 100, HttpStatusCode = 400, ResponseCode = "01", Description = "Dup", ServiceName = "Svc" };
        _repoMock.Setup(r => r.GetByCodeAsync("DUP_CODE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorCodeEntry { Code = "DUP_CODE" });

        var ex = await Assert.ThrowsAsync<ErrorCodeDuplicateException>(() => _sut.CreateAsync(request));
        Assert.Equal(ErrorCodes.ErrorCodeDuplicateValue, ex.ErrorValue);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsErrorCodeNotFoundException()
    {
        _repoMock.Setup(r => r.GetByCodeAsync("MISSING", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorCodeEntry?)null);

        var ex = await Assert.ThrowsAsync<ErrorCodeNotFoundException>(() => _sut.DeleteAsync("MISSING"));
        Assert.Equal(ErrorCodes.ErrorCodeNotFoundValue, ex.ErrorValue);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsErrorCodeNotFoundException()
    {
        _repoMock.Setup(r => r.GetByCodeAsync("NOPE", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorCodeEntry?)null);

        await Assert.ThrowsAsync<ErrorCodeNotFoundException>(
            () => _sut.UpdateAsync("NOPE", new UpdateErrorCodeRequest { Description = "x" }));
    }

    [Fact]
    public async Task CrudRoundTrip_CreateListUpdateDelete()
    {
        var entity = new ErrorCodeEntry
        {
            Code = "ROUND", Value = 500, HttpStatusCode = 500,
            ResponseCode = "50", Description = "Round trip", ServiceName = "Svc"
        };
        _repoMock.Setup(r => r.GetByCodeAsync("ROUND", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorCodeEntry?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<ErrorCodeEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorCodeEntry e, CancellationToken _) => e);

        // Create
        var created = (ErrorCodeResponse)await _sut.CreateAsync(new CreateErrorCodeRequest
        {
            Code = "ROUND", Value = 500, HttpStatusCode = 500,
            ResponseCode = "50", Description = "Round trip", ServiceName = "Svc"
        });
        Assert.Equal("ROUND", created.Code);

        // List
        _repoMock.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { entity });
        var list = (await _sut.ListAsync()).ToList();
        Assert.Single(list);

        // Update
        _repoMock.Setup(r => r.GetByCodeAsync("ROUND", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        var updated = (ErrorCodeResponse)await _sut.UpdateAsync("ROUND", new UpdateErrorCodeRequest { Description = "Updated" });
        Assert.Equal("Updated", updated.Description);

        // Delete
        await _sut.DeleteAsync("ROUND");
        _repoMock.Verify(r => r.RemoveAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }
}

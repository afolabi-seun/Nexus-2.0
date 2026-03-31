using BillingService.Api.Controllers;
using BillingService.Application.DTOs;
using BillingService.Application.DTOs.Subscriptions;
using BillingService.Domain.Interfaces.Services.Subscriptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BillingService.Tests.Unit.Controllers;

public class SubscriptionControllerTests
{
    private readonly Mock<ISubscriptionService> _mockService = new();

    private SubscriptionController CreateController(Guid orgId)
    {
        var controller = new SubscriptionController(_mockService.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Items["organizationId"] = orgId.ToString();
        httpContext.Items["CorrelationId"] = "test-corr";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task Create_Returns201WithApiResponse()
    {
        var orgId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subResponse = new SubscriptionResponse(
            Guid.NewGuid(), orgId, planId, "Starter", "starter",
            "Trialing", DateTime.UtcNow, DateTime.UtcNow.AddMonths(1),
            DateTime.UtcNow.AddDays(14), null, null, null);

        _mockService.Setup(s => s.CreateAsync(orgId, It.IsAny<CreateSubscriptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subResponse);

        var controller = CreateController(orgId);
        var result = await controller.Create(new CreateSubscriptionRequest(planId, null), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(201, objectResult.StatusCode);

        var apiResponse = Assert.IsType<ApiResponse<object>>(objectResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("test-corr", apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetCurrent_Returns200()
    {
        var orgId = Guid.NewGuid();
        var mockResult = new { test = "data" };

        _mockService.Setup(s => s.GetCurrentAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        var controller = CreateController(orgId);
        var result = await controller.GetCurrent(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("00", apiResponse.ResponseCode);
    }

    [Fact]
    public async Task Cancel_Returns200WithApiResponse()
    {
        var orgId = Guid.NewGuid();
        var subResponse = new SubscriptionResponse(
            Guid.NewGuid(), orgId, Guid.NewGuid(), "Starter", "starter",
            "Cancelled", DateTime.UtcNow, null, null, DateTime.UtcNow, null, null);

        _mockService.Setup(s => s.CancelAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subResponse);

        var controller = CreateController(orgId);
        var result = await controller.Cancel(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);
    }
}

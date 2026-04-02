using BillingService.Api.Extensions;
using BillingService.Application.DTOs;
using BillingService.Domain.Exceptions;
using BillingService.Infrastructure.Services.Stripe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingService.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks/stripe")]
[AllowAnonymous]
public class StripeWebhookController : ControllerBase
{
    private readonly StripeWebhookService _webhookService;

    public StripeWebhookController(StripeWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook(CancellationToken ct)
    {
        var signatureHeader = Request.Headers["Stripe-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(signatureHeader))
            throw new InvalidWebhookSignatureException();

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);

        await _webhookService.ProcessWebhookAsync(payload, signatureHeader, ct);

        return ApiResponse<object>.Ok(new { }, "Webhook processed.").ToActionResult(HttpContext);
    }
}

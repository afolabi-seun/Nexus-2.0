using BillingService.Domain.Exceptions;
using BillingService.Domain.Interfaces.Services;
using BillingService.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace BillingService.Infrastructure.Services.Stripe;

public class StripePaymentService : IStripePaymentService
{
    private readonly AppSettings _appSettings;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(AppSettings appSettings, ILogger<StripePaymentService> logger)
    {
        _appSettings = appSettings;
        _logger = logger;
        StripeConfiguration.ApiKey = appSettings.StripeSecretKey;
    }

    public async Task<(string customerId, string subscriptionId)> CreateSubscriptionAsync(
        Guid organizationId, string planCode, decimal priceMonthly, string? paymentMethodToken, CancellationToken ct)
    {
        try
        {
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Metadata = new Dictionary<string, string> { { "organizationId", organizationId.ToString() } },
                PaymentMethod = paymentMethodToken,
                InvoiceSettings = paymentMethodToken is not null
                    ? new CustomerInvoiceSettingsOptions { DefaultPaymentMethod = paymentMethodToken }
                    : null
            }, cancellationToken: ct);

            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.CreateAsync(new SubscriptionCreateOptions
            {
                Customer = customer.Id,
                Items = [new SubscriptionItemOptions { Price = $"price_{planCode}_monthly" }],
                TrialPeriodDays = 14,
                Metadata = new Dictionary<string, string> { { "organizationId", organizationId.ToString() } }
            }, cancellationToken: ct);

            return (customer.Id, subscription.Id);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating subscription for org {OrgId}", organizationId);
            throw new PaymentProviderException($"Stripe error: {ex.Message}");
        }
    }

    public async Task UpdateSubscriptionAsync(
        string externalSubscriptionId, string newPlanCode, decimal newPriceMonthly, CancellationToken ct)
    {
        try
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(externalSubscriptionId, cancellationToken: ct);

            await subscriptionService.UpdateAsync(externalSubscriptionId, new SubscriptionUpdateOptions
            {
                Items =
                [
                    new SubscriptionItemOptions
                    {
                        Id = subscription.Items.Data[0].Id,
                        Price = $"price_{newPlanCode}_monthly"
                    }
                ],
                ProrationBehavior = "create_prorations"
            }, cancellationToken: ct);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error updating subscription {SubId}", externalSubscriptionId);
            throw new PaymentProviderException($"Stripe error: {ex.Message}");
        }
    }

    public async Task CancelSubscriptionAtPeriodEndAsync(string externalSubscriptionId, CancellationToken ct)
    {
        try
        {
            var subscriptionService = new SubscriptionService();
            await subscriptionService.UpdateAsync(externalSubscriptionId, new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            }, cancellationToken: ct);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error cancelling subscription {SubId}", externalSubscriptionId);
            throw new PaymentProviderException($"Stripe error: {ex.Message}");
        }
    }

    public bool VerifyWebhookSignature(string payload, string signatureHeader, out object? stripeEvent)
    {
        try
        {
            var evt = EventUtility.ConstructEvent(payload, signatureHeader, _appSettings.StripeWebhookSecret);
            stripeEvent = evt;
            return true;
        }
        catch (StripeException)
        {
            stripeEvent = null;
            return false;
        }
    }
}

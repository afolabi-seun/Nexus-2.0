# Notification System

## Overview

UtilityCoreService handles all notification dispatch for the platform. Notifications arrive via the Redis outbox from all 4 consuming services and are dispatched through independently toggleable channels.

```
ProfileCoreService  ─┐
SecurityCoreService ─┤  Redis outbox    ┌──────────────────────┐
TransactionService  ─┼─────────────────►│  UtilityCoreService  │
WalletCoreService   ─┘  (async)        │  NotificationDispatch │
                                        └──────┬───────────────┘
                                               │
                              ┌─────────────────┼─────────────────┐
                              ▼                 ▼                 ▼
                         ┌─────────┐      ┌──────────┐     ┌──────────┐
                         │  Email  │      │   SMS    │     │   Push   │
                         │  SMTP   │      │  HTTP GW │     │  HTTP GW │
                         └─────────┘      └──────────┘     └──────────┘
```

---

## Channels

| Channel | Toggle | Transport | Status |
|---------|--------|-----------|--------|
| Email | `EMAIL_ENABLED=true` | SMTP (Gmail) | Active |
| SMS | `SMS_ENABLED=false` | HTTP gateway | Stub (not yet integrated) |
| Push | `PUSH_ENABLED=false` | HTTP gateway | Stub (not yet integrated) |

Disabled channels are **silently skipped** — no errors, no failed notification logs. Enable a channel by setting its toggle to `true` in `.env` and providing the required credentials.

### Email Configuration (.env)

```
EMAIL_ENABLED=true
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
EMAIL_SENDER_ADDRESS=noreply@finsyswallet.com
EMAIL_SENDER_NAME=FinSys Wallet
```

### SMS/Push Configuration (.env)

```
SMS_ENABLED=false
SMS_GATEWAY_URL=https://sms-provider.example.com/api/send
SMS_GATEWAY_API_KEY=your-api-key

PUSH_ENABLED=false
PUSH_GATEWAY_URL=https://push-provider.example.com/api/send
PUSH_GATEWAY_API_KEY=your-api-key
```

---

## Notification Types

| Type | Channels | Triggered By | Template Variables |
|------|----------|-------------|-------------------|
| `CredentialGenerated` | SMS, Email | SME onboarding, staff/customer creation | `Username`, `TemporaryPassword` |
| `WelcomeAdmin` | SMS, Email | SME onboarding | `FirstName` |
| `WelcomeCustomer` | SMS, Email | Customer creation | `FirstName`, `WalletName` |
| `OtpCode` | SMS, Email | Password reset, OTP request | `OtpCode`, `ExpiryMinutes` |
| `PasswordReset` | SMS, Email | Password reset flow | `OtpCode`, `ExpiryMinutes` |
| `InviteLink` | SMS, Email | Invite creation | `SmeName`, `InviteUrl` |
| `CreditAlert` | SMS, Email | Wallet credit | `Amount`, `WalletName`, `Balance`, `TransactionRef` |
| `DebitAlert` | SMS, Email | Wallet debit | `Amount`, `WalletName`, `Balance`, `TransactionRef` |
| `LowBalance` | SMS, Email | Balance below threshold | `WalletName`, `Balance`, `Threshold` |
| `SpendingLimit` | SMS, Email | Spending limit reached | `WalletName`, `LimitType`, `Limit` |
| `SecurityAlert` | SMS, Email | Suspicious login, PIN locked | `Description` |
| `PaymentLinkGenerated` | SMS, Email | Payment link creation | `Amount`, `PaymentUrl`, `ExpiryDate` |
| `QrPaymentGenerated` | SMS, Email | QR payment creation | `Amount`, `PaymentRef` |
| `VirtualAccountDetails` | SMS, Email | Virtual account creation | `AccountNumber`, `BankName`, `WalletName` |

---

## Templates

### SMS Templates

Plain text with `{placeholder}` tokens, kept under 160 characters where possible:

```csharp
// Templates/Sms/SmsTemplates.cs
public const string OtpCode =
    "Your FinSys Wallet verification code is {OtpCode}. Expires in {ExpiryMinutes} min. Do not share.";

public const string CreditAlert =
    "Credit: NGN {Amount} to {WalletName}. Bal: NGN {Balance}. Ref: {TransactionRef}";

public const string WelcomeCustomer =
    "Welcome to FinSys Wallet, {FirstName}! Your wallet ({WalletName}) is ready. " +
    "Start sending and receiving payments today.";
```

### Email Templates

HTML templates with a shared base layout and per-type body templates:

```
Templates/Emails/
├── Layout/
│   └── BaseLayout.html          ← Shared header/footer/branding
└── Body/
    ├── CredentialGenerated.html
    ├── WelcomeCustomer.html
    ├── WelcomeAdmin.html
    ├── OtpCode.html
    ├── PasswordReset.html
    ├── InviteLink.html
    ├── CreditAlert.html
    ├── DebitAlert.html
    ├── PaymentLinkGenerated.html
    └── VirtualAccountDetails.html
```

The `ITemplateRendererService` renders email templates by:
1. Loading the body template for the notification type
2. Substituting `{placeholder}` tokens with template variables
3. Injecting the rendered body into the base layout

If a template is not found, the service falls back to the raw body text.

---

## Dispatch Flow

### Step 1: Publishing Service Sends Event

Any service publishes a notification event to its Redis outbox:

```csharp
// Example: ProfileCoreService after creating a customer
var notificationEvent = new
{
    Type = "notification",
    Payload = new
    {
        TenantId = tenantId,
        UserId = customer.CustId,
        NotificationType = "WelcomeCustomer",
        Channel = "SMS,Email",
        Recipient = customer.PhoneNo,
        Subject = "Welcome to FinSys Wallet",
        CorrelationId = correlationId,
        TemplateVariables = new Dictionary<string, string>
        {
            ["FirstName"] = customer.FirstName,
            ["WalletName"] = walletResponse.WalletName
        }
    }
};
await outboxService.PublishAsync(RedisKeys.Outbox, JsonSerializer.Serialize(notificationEvent));
```

### Step 2: OutboxProcessor Routes to Dispatch

UtilityCoreService's `OutboxProcessorService` picks up the event and routes it:

```csharp
case "notification":
    await HandleNotificationEventAsync(scope, envelope.Payload);
    break;
```

### Step 3: NotificationDispatchService Processes

```
NotificationEventRequest received
  │
  ▼
ResolveChannels("SMS,Email") → ["SMS", "Email"]
  │
  ▼
For each channel:
  ├── IsChannelEnabled? (check .env toggle)
  │   ├── No → skip silently
  │   └── Yes ↓
  ├── Create NotificationLog entry (status: Pending)
  ├── Render template (SMS: token substitution, Email: HTML render)
  ├── Send via channel transport
  │   ├── Email → SMTP (SmtpClient)
  │   └── SMS/Push → HTTP gateway (POST with X-Api-Key)
  ├── Success → Update log status to Sent
  └── Failure → Update log status to Failed (eligible for retry)
```

### Step 4: Notification Log Entry

Every dispatch attempt is logged in the `notification_log` table:

| Column | Example |
|--------|---------|
| `notification_log_id` | `a1b2c3d4-...` |
| `tenant_id` | `11111111-...` |
| `user_id` | `e5f6a7b8-...` |
| `notification_type` | `WelcomeCustomer` |
| `channel` | `Email` |
| `recipient` | `adebayo@example.com` |
| `subject` | `Welcome to FinSys Wallet` |
| `status` | `Sent` / `Failed` / `PermanentlyFailed` |
| `retry_count` | `0` |
| `last_retry_date` | `null` |
| `reference_code` | `NTF-20250615-A1B2C3D4` |

Queryable via:
```
GET /api/v1/notification-logs   (paginated, PlatformAdmin only)
```

---

## Retry with Exponential Backoff

Failed notifications are retried by `NotificationRetryHostedService`, a background service that polls periodically.

### Configuration

```
NOTIFICATION_RETRY_INTERVAL_MINUTES=10   (poll interval, default: 10)
```

### Retry Logic

```
Poll every NOTIFICATION_RETRY_INTERVAL_MINUTES:
  │
  ▼
Query: notification_log WHERE status = 'Failed' AND retry_count < 3
  │
  ▼
For each failed notification:
  │
  ├── Calculate backoff: 2^retryCount minutes
  │   Retry 1: after 2 min
  │   Retry 2: after 4 min
  │   Retry 3: after 8 min
  │
  ├── Is it time to retry? (now >= lastRetryDate + backoff)
  │   ├── No → skip
  │   └── Yes ↓
  │
  ├── Re-dispatch via NotificationDispatchService (skipLogging: true)
  │   ├── Success → status = Sent, retryCount++
  │   └── Failure → retryCount++
  │       ├── retryCount < 3 → status stays Failed (retry again later)
  │       └── retryCount >= 3 → status = PermanentlyFailed (give up)
  │
  ▼
SaveChangesAsync (batch update all processed notifications)
```

### Status Lifecycle

```
Pending → Sent                    (first attempt succeeds)
Pending → Failed → Sent           (retry succeeds)
Pending → Failed → Failed → Sent  (second retry succeeds)
Pending → Failed → Failed → Failed → PermanentlyFailed  (all 3 retries exhausted)
```

### Key Design Decisions

- `skipLogging: true` on retry — prevents creating duplicate log entries; the original entry is updated in place
- Exponential backoff — avoids hammering a failing gateway
- Max 3 retries — prevents infinite retry loops
- `PermanentlyFailed` status — makes it easy to query and investigate delivery failures
- Poll-based (not event-driven) — simpler, no additional infrastructure needed

---

## Multi-Channel Dispatch

A single notification event can target multiple channels:

```json
{
  "channel": "SMS,Email"
}
```

Each channel is dispatched independently:
- If Email succeeds but SMS fails, Email is logged as `Sent` and SMS as `Failed`
- The SMS failure is retried independently
- If `channel` is empty/null, defaults to `["SMS", "Email"]`

---

## How Services Publish Notifications

Services don't call UtilityCoreService directly for notifications. They publish to the Redis outbox, and the outbox processor handles dispatch:

```csharp
// ProfileCoreService: after customer creation
await _outboxService.PublishAsync(RedisKeys.Outbox, JsonSerializer.Serialize(new
{
    Type = "notification",
    Payload = new NotificationEventRequest
    {
        TenantId = tenantId,
        UserId = customer.CustId,
        NotificationType = "CredentialGenerated",
        Channel = "SMS,Email",
        Recipient = customer.PhoneNo,
        Subject = "Your FinSys Wallet Credentials",
        TemplateVariables = new Dictionary<string, string>
        {
            ["Username"] = customer.Username,
            ["TemporaryPassword"] = tempPassword
        }
    }
}));
```

This means:
- Notification dispatch is **non-blocking** — the API response returns immediately
- If UtilityCoreService is down, events queue in Redis and are processed when it recovers
- The publishing service doesn't need to know about channels, templates, or delivery status

---

## Notification Endpoints

| Endpoint | Auth | Description |
|----------|------|-------------|
| `GET /api/v1/notification-logs` | PlatformAdmin | Paginated list of all notification logs |
| `POST /api/v1/notifications/dispatch` | Service auth | Direct dispatch (used internally by outbox processor) |

The dispatch endpoint exists for direct HTTP calls but is primarily used internally. All normal notification flow goes through the Redis outbox.

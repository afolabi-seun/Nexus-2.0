# Environment Configuration

This directory contains environment-specific configuration files for the Nexus 2.0 platform.

## Structure

```
config/
├── development/     # Local development (default)
├── staging/         # Staging/QA environment
├── production/      # Production environment
└── README.md
```

## Usage

1. Copy the appropriate environment folder's `.env` files to each service's Api directory:
   ```bash
   # Example: set up for staging
   cp config/staging/security-service.env src/backend/SecurityService/SecurityService.Api/.env
   cp config/staging/profile-service.env src/backend/ProfileService/ProfileService.Api/.env
   cp config/staging/work-service.env src/backend/WorkService/WorkService.Api/.env
   cp config/staging/utility-service.env src/backend/UtilityService/UtilityService.Api/.env
   cp config/staging/billing-service.env src/backend/BillingService/BillingService.Api/.env
   cp config/staging/frontend.env src/frontend/.env
   ```

2. Or use the setup script:
   ```bash
   ./config/setup-env.sh development   # or staging, production
   ```

## Environment Differences

| Setting | Development | Staging | Production |
|---------|------------|---------|------------|
| Database Host | localhost | staging-db.internal | prod-db.internal |
| Redis Host | localhost | staging-redis.internal | prod-redis.internal |
| Seq URL | localhost:5341 | staging-seq.internal:5341 | prod-seq.internal:5341 |
| Frontend URL | localhost:5173 | staging.nexus.example.com | app.nexus.example.com |
| JWT Expiry | 15 min / 7 days | 15 min / 7 days | 10 min / 1 day |
| Rate Limits | Relaxed | Standard | Strict |
| Stripe | Test keys | Test keys | Live keys |
| Log Level | Information | Information | Warning |

## Security Notes

- Never commit actual `.env` files to version control
- Production secrets should be managed via a secrets manager (AWS Secrets Manager, Azure Key Vault, etc.)
- The `.env` files in this directory use placeholder values — replace them before use

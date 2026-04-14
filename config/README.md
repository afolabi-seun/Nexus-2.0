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

```bash
# Set up for a specific environment
./config/setup-env.sh development   # or staging, production
```

## Environment Differences

| Setting | Development | Staging | Production |
|---------|------------|---------|------------|
| Database Host | localhost | staging-db.internal | prod-db.internal |
| Redis Host | localhost | staging-redis.internal | prod-redis.internal |
| Frontend URL | localhost:5173 | staging.nexus.example.com | app.nexus.example.com |
| JWT Expiry | 15 min / 7 days | 15 min / 7 days | 10 min / 1 day |
| Rate Limits | Relaxed | Standard | Strict |
| Stripe | Test keys | Test keys | Live keys |
| SMTP | Mailpit (localhost:1025) | Mailpit (localhost:1025) | AWS SES (port 587, SSL) |
| Log Level | Information | Information | Warning |

## Security Notes

- Never commit actual `.env` files to version control
- Production secrets should be managed via a secrets manager
- Use `./config/setup-env.sh` to deploy configs to each service

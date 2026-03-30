#!/bin/bash
# =============================================================================
# Nexus 2.0 — Environment Setup Script
# Usage: ./config/setup-env.sh [development|staging|production]
# =============================================================================

set -e

ENV="${1:-development}"

if [[ ! -d "config/$ENV" ]]; then
    echo "Error: Environment '$ENV' not found. Use: development, staging, or production"
    exit 1
fi

echo "Setting up $ENV environment..."

cp "config/$ENV/security-service.env" "src/backend/SecurityService/SecurityService.Api/.env"
echo "  ✓ SecurityService"

cp "config/$ENV/profile-service.env" "src/backend/ProfileService/ProfileService.Api/.env"
echo "  ✓ ProfileService"

cp "config/$ENV/work-service.env" "src/backend/WorkService/WorkService.Api/.env"
echo "  ✓ WorkService"

cp "config/$ENV/utility-service.env" "src/backend/UtilityService/UtilityService.Api/.env"
echo "  ✓ UtilityService"

cp "config/$ENV/billing-service.env" "src/backend/BillingService/BillingService.Api/.env"
echo "  ✓ BillingService"

cp "config/$ENV/frontend.env" "src/frontend/.env"
echo "  ✓ Frontend"

echo ""
echo "Done! $ENV environment configured for all services."
if [[ "$ENV" != "development" ]]; then
    echo ""
    echo "⚠️  Remember to replace all CHANGE_ME values with actual secrets!"
fi

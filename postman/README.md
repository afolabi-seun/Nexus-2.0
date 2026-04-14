# Postman Collection — Nexus 2.0

## Files

| File | Description |
|------|-------------|
| `nexus-2.0-api.postman_collection.json` | API collection covering all 5 microservices (90 public endpoints, internal service-to-service endpoints excluded) |
| `nexus-2.0-development.postman_environment.json` | Environment variables for local development (localhost) |
| `nexus-2.0-staging.postman_environment.json` | Environment variables for staging |

## Quick Start

1. Open Postman
2. Import `nexus-2.0-api.postman_collection.json`
3. Import `nexus-2.0-development.postman_environment.json`
4. Select the "Nexus 2.0 — Development" environment
5. Run the "Login" request — tokens are auto-extracted into environment variables
6. All other requests use `Bearer {{access_token}}` automatically

## Collection Structure

- **SecurityService** — Auth (login, refresh, logout, OTP), Password (change, reset), Sessions
- **ProfileService** — Organizations, Team Members, Departments, Invites, Devices, Preferences, Notification Settings, Navigation
- **WorkService** — Projects, Stories, Sprints, Boards, Comments, Search, Reports, Time Tracking, Analytics, Risk Register
- **UtilityService** — Audit Logs, Notification Logs, Reference Data, Error Codes
- **BillingService** — Plans, Subscriptions, Usage, Admin Billing, Admin Plans

> **Note:** Internal service-to-service endpoints (credentials/generate, feature-gates, service-tokens, etc.) are excluded from this collection. They are hidden from Swagger via `HideServiceAuthFilter`.

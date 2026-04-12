# Nexus 2.0 — TODO

Prioritized roadmap for hardening, features, and documentation before release.

---

## Phase 1 — Security & Access Control

- [x] **Endpoint role restriction audit** — Audit every controller endpoint across all 5 services. Ensure Admin/Owner/Member distinctions are enforced. No endpoint should be accidentally open to any authenticated user.
- [x] **PlatformAdmin role** — Super-admin role above org-level roles in SecurityService. Capabilities: manage all orgs (suspend/delete/audit), platform-wide analytics, manage subscription plans & feature gates, user impersonation for support, access to internal/diagnostic endpoints.
- [x] **Hide `[ServiceAuth]` endpoints from Swagger** — Add a Swagger document filter to exclude service-to-service internal endpoints from public API docs.
- [x] **Remove internal endpoints from Postman** — Remove credentials/generate, by-username, and other internal endpoints from the public Postman collection. Optionally maintain a separate internal/dev collection.
- [x] **Endpoint restrictions reference doc** — Comprehensive markdown file (`docs/endpoint-restrictions.md`) listing every endpoint, its HTTP method, required role, and auth type (user JWT, service-to-service, public).

---

## Phase 2 — Communications

- [ ] **Email integration** — Set up email sending (e.g., SendGrid, SES). Emails needed for:
  - Account registration (welcome / email verification)
  - OTP delivery (login, password reset)
  - Organization invite (join link)
  - Password reset confirmation
  - Subscription/billing events (plan change, payment failed, invoice)
- [ ] **Sprint notifications** — Background service (daily schedule) for:
  - Sprint due soon (e.g., 2 days before end date)
  - Sprint overdue (past end date, still active)
  - Sprint at risk (too many incomplete stories relative to time remaining)
  - Story blocked / stuck notifications

---

## Phase 3 — Documentation

- [ ] **QA guide** — Manual QA checklist covering happy paths and edge cases per feature. Include role restriction scenarios (what each role can/cannot do).
- [ ] **Testing guide with realistic seed data** — Seed script or guide with realistic orgs, departments, members, projects, stories, sprints. Include role restriction test scenarios for each role type.
- [ ] **Update main README** — Reflect new features, phases, role system, email integration, and link to new docs (QA guide, testing guide, endpoint restrictions).
- [ ] **Update QA email templates** — Include role restriction context in QA-related email communications.

---

## Phase 4 — Hardening

- [ ] **API rate limiting audit** — Verify rate limits per endpoint (auth endpoints stricter, read endpoints more lenient).
- [ ] **Input validation audit** — Ensure FluentValidation covers all edge cases (max lengths, special characters, injection attempts).
- [ ] **CORS lockdown** — Production config only allows the frontend origin.
- [ ] **Error message sanitization** — Production error responses must not leak stack traces or internal details.
- [ ] **Pagination max limits** — All list endpoints have sensible max page sizes to prevent abuse.
- [ ] **Soft delete audit** — Deleted entities (orgs, members, projects) properly excluded from all queries.

---

## Phase 5 — Enhancements

- [ ] **Health check endpoints** — `/health` on each service for monitoring and Docker health checks.
- [ ] **API versioning** — Add `v1` prefix to all routes now (easier before release than after).
- [ ] **Export functionality** — CSV/PDF export for stories, sprint reports, time tracking, invoices.
- [ ] **Activity feed** — Per-project or per-org feed showing recent actions (builds on existing audit log).
- [ ] **Bulk operations** — Bulk move stories between sprints, bulk assign, bulk status change.
- [ ] **Story templates** — Reusable templates for common story types (bug report, feature request, tech debt).
- [ ] **SLA tracking** — Time-to-resolution metrics, especially for bug stories.
- [ ] **Archival** — Archive completed sprints/projects to keep active views clean, preserve history.
- [ ] **Webhook support** — Let orgs configure webhooks for key events (Slack, Teams integration).
- [ ] **Global search** — Search across stories, projects, members.
- [ ] **Onboarding flow** — Guided setup for new orgs (create first project, invite members, create first sprint).
- [ ] **Keyboard shortcuts** — For Kanban board and common frontend actions.
- [ ] **Offline indicator** — Frontend shows connection status, queues actions when offline.

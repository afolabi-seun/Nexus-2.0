# Nexus 2.0 — Niche Pivot Roadmap
## Goal: "The agile platform built for African dev teams and agencies"

---

## What Already Exists (No Work Needed)

These are competitive advantages you already have:

- ✅ Multi-tenant architecture (5 microservices, isolated databases)
- ✅ RBAC with 5 roles (PlatformAdmin → Viewer)
- ✅ Stripe billing with feature gates and usage tracking
- ✅ Time tracking with cost rates, start/stop timer, approval workflows
- ✅ Analytics — velocity, resource management, project cost, health scoring, risk register, dependency analysis, bug metrics
- ✅ Sprint management with background notifications (due soon, overdue, at risk)
- ✅ Activity feed with paginated history
- ✅ CSV export for stories and time entries
- ✅ Bulk operations (status update, assign)
- ✅ Email notifications (SMTP, Mailpit dev / SES+SendGrid prod)
- ✅ 663 tests (570 backend + 93 frontend)
- ✅ CI/CD (GitHub Actions → build, test, Docker push)
- ✅ Docker Compose deployment (3 configs: full, local, server)
- ✅ Polly resilience (retry, circuit breaker, timeout)
- ✅ Structured logging (Serilog → Seq)

---

## Phase 1: Core Differentiators (4-6 weeks)

These three features create the sharpest wedge against Jira/Linear/Asana for the African market.

### 1.1 WhatsApp Notifications (~1-2 weeks)
**Why:** African teams live on WhatsApp, not Slack or email. This is the single biggest differentiator.

**What exists:** UtilityService handles notifications + background service for sprint alerts. Email channel already works.

**What to build:**
- [ ] Integrate WhatsApp Business API (via Twilio or Meta Business API)
- [ ] Add WhatsApp as a notification channel in UtilityService
- [ ] Route sprint notifications (due soon, overdue, at risk) to WhatsApp
- [ ] Route task assignment and status change notifications to WhatsApp
- [ ] Add user preference: choose notification channel (email, WhatsApp, both)
- [ ] SMS fallback via Twilio for users without WhatsApp
- [ ] Test message delivery and formatting

**Architecture notes:**
- New WhatsApp client in UtilityService.Infrastructure/Services/WhatsApp/
- Notification channel selection stored in user profile (ProfileService)
- UtilityService reads channel preference when dispatching notifications

### 1.2 Invoice Generation from Time Tracking (~1-2 weeks)
**Why:** Agencies bill clients by the hour. Time tracking exists — just need to turn approved hours into invoices. Eliminates the need for a separate invoicing tool.

**What exists:** Time entries, cost rates, approval workflows, CSV export — all in WorkService.

**What to build:**
- [ ] Invoice data model (invoice number, client, line items from time entries, totals, tax, status)
- [ ] "Generate Invoice" endpoint — pulls approved time entries for a project/date range
- [ ] Invoice PDF generation (client name, project, line items, hours, rates, totals)
- [ ] Invoice list view in frontend (draft, sent, paid, overdue)
- [ ] Mark invoice as sent/paid
- [ ] Email invoice to client as PDF attachment
- [ ] WhatsApp invoice delivery (send PDF or payment link)

**Architecture notes:**
- Invoice entity in WorkService.Domain (it's tied to time entries and projects)
- PDF generation via a lightweight library (QuestPDF or similar)
- Invoice status tracking in WorkService database

### 1.3 Mobile Money / Local Payments (~2-3 weeks)
**Why:** Most African businesses don't have credit cards. Mobile money (M-Pesa, MTN MoMo) is how they pay. Stripe alone won't work for this market.

**What exists:** BillingService is already its own microservice with Stripe integration, feature gates, and usage tracking.

**What to build:**
- [ ] Integrate Paystack or Flutterwave SDK (both support M-Pesa, MTN MoMo, bank transfer, cards across Africa)
- [ ] Add payment provider abstraction in BillingService (so Stripe and Paystack/Flutterwave coexist)
- [ ] Support local currency billing (KES, NGN, GHS, ZAR, UGX)
- [ ] Mobile money checkout flow in frontend
- [ ] Webhook handlers for Paystack/Flutterwave payment confirmations
- [ ] Update subscription management to work with both providers
- [ ] Test payment flows end-to-end for each payment method

**Architecture notes:**
- Payment provider interface in BillingService.Domain
- Stripe and Paystack/Flutterwave implementations in BillingService.Infrastructure/Services/
- Provider selection based on organization's country/preference
- Webhook endpoints in BillingService.Api

---

## Phase 2: Agency-Focused Features (4-6 weeks after Phase 1)

These make Nexus specifically valuable for agencies managing client work.

### 2.1 Client Portal (~2-3 weeks)
- [ ] External client role (no login required, access via unique link)
- [ ] Client can view project progress, milestones, deliverables
- [ ] Client can approve/reject deliverables
- [ ] Client can leave comments/feedback
- [ ] Branded portal (agency logo, colors)
- [ ] Client-facing progress reports (auto-generated from sprint/story data)

### 2.2 Contractor / Freelancer Access (~1-2 weeks)
- [ ] Contractor role — invited to specific projects with limited access
- [ ] Time tracking scoped to assigned tasks only
- [ ] Contractor payment tracking (hours × rate, payment status)
- [ ] Contractor can't see other projects, billing, or org settings

### 2.3 Professional Reporting (~1-2 weeks)
- [ ] Branded PDF reports (agency logo, client name)
- [ ] Sprint summary report (completed, in progress, blocked)
- [ ] Monthly project status report
- [ ] Export to PDF (in addition to existing CSV)
- [ ] Scheduled report delivery (email + WhatsApp)

---

## Phase 3: Nice-to-Haves (Backlog)

Lower priority — build these based on user feedback after launch.

### 3.1 AI Standup Summaries
- [ ] Auto-generate daily summary from task activity (what was done, what's in progress, what's blocked)
- [ ] Deliver via WhatsApp every morning
- [ ] Weekly summary for managers

### 3.2 Telegram Bot Integration
- [ ] Task notifications via Telegram
- [ ] Quick actions (update status, log time) from Telegram

### 3.3 Offline-First / Low-Bandwidth Mode
- [ ] Service worker for offline access to boards and task lists
- [ ] Queue actions offline, sync when connected
- [ ] Compressed API responses for low-bandwidth connections

### 3.4 Flexible Billing (Weekly / Pay-As-You-Go)
- [ ] Weekly billing option for small teams
- [ ] Pay-per-project pricing for freelancers
- [ ] Trial periods without requiring payment method

---

## Effort Summary

| Phase | Features | Estimated Time |
|-------|----------|---------------|
| Phase 1 | WhatsApp, Invoicing, Mobile Money | 4-6 weeks |
| Phase 2 | Client Portal, Contractors, Reports | 4-6 weeks |
| Phase 3 | AI Summaries, Telegram, Offline, Flex Billing | Backlog |

**Total to a launchable, differentiated product: ~8-12 weeks from today.**

---

## Target Market

- African dev teams and software agencies (Nigeria, Kenya, South Africa, Ghana, Uganda)
- Small-to-mid agencies (5-50 people) managing multiple client projects
- Teams currently using Jira/Trello/Asana but frustrated by pricing, payment methods, or lack of local relevance
- Freelancer collectives who need lightweight project management + invoicing

## Pricing Strategy (Draft)

- Free: up to 3 users, 1 project (get them in the door)
- Team: ~$5-8/user/month in local currency equivalent (core features + WhatsApp notifications)
- Agency: ~$12-15/user/month (client portal, invoicing, contractor access, branded reports)
- Pay in mobile money, card, or bank transfer

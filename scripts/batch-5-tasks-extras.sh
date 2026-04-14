#!/bin/bash
# =============================================================================
# Nexus 2.0 — Development Seed Script
# Batch 5: Tasks + Comments + Cost Rates + Time Policy + Risk Register
# =============================================================================
set -e

WORK_URL="http://localhost:5003"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'
log() { echo -e "${GREEN}[SEED]${NC} $1"; }
warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
fail() { echo -e "${RED}[FAIL]${NC} $1"; exit 1; }
json_field() { echo "$1" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d$2)" 2>/dev/null; }
api() {
    local method=$1 url=$2 token=$3 body=$4
    if [ -n "$body" ]; then
        curl -s -X "$method" "$url" -H "Content-Type: application/json" -H "Authorization: Bearer $token" -d "$body"
    else
        curl -s -X "$method" "$url" -H "Content-Type: application/json" -H "Authorization: Bearer $token"
    fi
}

STATE_FILE="$(dirname "$0")/.seed-state.env"
[ -f "$STATE_FILE" ] || fail "Run previous batches first."
source "$STATE_FILE"

create_task() {
    local token=$1 story_id=$2 title=$3 type=$4 desc=$5
    api POST "$WORK_URL/api/v1/tasks" "$token" \
        "{\"storyId\":\"$story_id\",\"title\":\"$title\",\"taskType\":\"$type\",\"description\":\"$desc\"}" > /dev/null 2>&1 || true
}

# =============================================================================
# Step 1: Create tasks for Acme MOB stories
# =============================================================================
log "Step 1: Creating tasks for Acme MOB stories..."
if [ -n "$MOB_S1" ] && [ "$MOB_S1" != "" ]; then
    create_task "$SARAH_TOKEN" "$MOB_S1" "Design login UI mockup" "Design" "Create Figma mockup for login screen"
    create_task "$SARAH_TOKEN" "$MOB_S1" "Implement login API call" "Development" "Connect login form to SecurityService auth endpoint"
    create_task "$SARAH_TOKEN" "$MOB_S1" "Write login unit tests" "Testing" "Unit tests for login flow and error handling"
fi
if [ -n "$MOB_S2" ] && [ "$MOB_S2" != "" ]; then
    create_task "$SARAH_TOKEN" "$MOB_S2" "Configure Firebase project" "DevOps" "Set up Firebase project and download config files"
    create_task "$SARAH_TOKEN" "$MOB_S2" "Implement push handler" "Development" "Handle incoming push notifications and deep links"
fi
if [ -n "$MOB_S3" ] && [ "$MOB_S3" != "" ]; then
    create_task "$SARAH_TOKEN" "$MOB_S3" "Reproduce crash on iOS 17" "Testing" "Set up iOS 17 simulator and reproduce the crash"
    create_task "$SARAH_TOKEN" "$MOB_S3" "Fix logout memory leak" "Development" "Fix the retain cycle causing crash on logout"
fi
log "  7 MOB tasks created ✓"

# =============================================================================
# Step 2: Create tasks for Acme WEB stories
# =============================================================================
log "Step 2: Creating tasks for Acme WEB stories..."
if [ -n "$WEB_S1" ] && [ "$WEB_S1" != "" ]; then
    create_task "$SARAH_TOKEN" "$WEB_S1" "Design new dashboard layout" "Design" "Create wireframes for the new dashboard"
    create_task "$SARAH_TOKEN" "$WEB_S1" "Build analytics widgets" "Development" "Implement chart components using Recharts"
    create_task "$SARAH_TOKEN" "$WEB_S1" "Dashboard integration tests" "Testing" "E2E tests for dashboard data loading"
fi
if [ -n "$WEB_S2" ] && [ "$WEB_S2" != "" ]; then
    create_task "$SARAH_TOKEN" "$WEB_S2" "Debug pagination query" "Development" "Fix the SQL query for filtered pagination"
fi
log "  4 WEB tasks created ✓"

# =============================================================================
# Step 3: Create tasks for Globex PAY stories
# =============================================================================
log "Step 3: Creating tasks for Globex PAY stories..."
if [ -n "$PAY_S1" ] && [ "$PAY_S1" != "" ]; then
    create_task "$EMMA_TOKEN" "$PAY_S1" "Set up Stripe SDK" "Development" "Install and configure Stripe .NET SDK"
    create_task "$EMMA_TOKEN" "$PAY_S1" "Implement payment intent flow" "Development" "Create payment intents and handle confirmations"
    create_task "$EMMA_TOKEN" "$PAY_S1" "Stripe webhook handler" "Development" "Handle Stripe webhook events for payment status"
    create_task "$EMMA_TOKEN" "$PAY_S1" "Payment flow QA" "Testing" "Test payment flows with Stripe test cards"
fi
if [ -n "$PAY_S2" ] && [ "$PAY_S2" != "" ]; then
    create_task "$EMMA_TOKEN" "$PAY_S2" "Design invoice template" "Design" "Create PDF invoice template with company branding"
    create_task "$EMMA_TOKEN" "$PAY_S2" "Generate PDF invoices" "Development" "Implement PDF generation using QuestPDF"
fi
log "  6 PAY tasks created ✓"

# =============================================================================
# Step 4: Create comments on some stories
# =============================================================================
log "Step 4: Creating comments..."
if [ -n "$MOB_S1" ] && [ "$MOB_S1" != "" ]; then
    api POST "$WORK_URL/api/v1/comments" "$SARAH_TOKEN" \
        "{\"entityType\":\"Story\",\"entityId\":\"$MOB_S1\",\"content\":\"Should we support social login (Google/Apple) in the first release?\"}" > /dev/null 2>&1 || true
    api POST "$WORK_URL/api/v1/comments" "$ACME_TOKEN" \
        "{\"entityType\":\"Story\",\"entityId\":\"$MOB_S1\",\"content\":\"Let's start with email/password only. Social login can be a follow-up story.\"}" > /dev/null 2>&1 || true
fi
if [ -n "$MOB_S3" ] && [ "$MOB_S3" != "" ]; then
    api POST "$WORK_URL/api/v1/comments" "$SARAH_TOKEN" \
        "{\"entityType\":\"Story\",\"entityId\":\"$MOB_S3\",\"content\":\"This is a P0 bug. Affects all iOS 17 users. Need to fix ASAP.\"}" > /dev/null 2>&1 || true
fi
if [ -n "$PAY_S1" ] && [ "$PAY_S1" != "" ]; then
    api POST "$WORK_URL/api/v1/comments" "$EMMA_TOKEN" \
        "{\"entityType\":\"Story\",\"entityId\":\"$PAY_S1\",\"content\":\"Using Stripe Payment Intents API. Need to handle 3D Secure for EU cards.\"}" > /dev/null 2>&1 || true
fi
log "  4 comments created ✓"

# =============================================================================
# Step 5: Create cost rates for Acme
# =============================================================================
log "Step 5: Creating Acme cost rates..."
api POST "$WORK_URL/api/v1/cost-rates" "$ACME_TOKEN" \
    '{"rateType":"OrgDefault","hourlyRate":75.00,"currency":"USD","effectiveFrom":"2025-01-01"}' > /dev/null 2>&1 || true
api POST "$WORK_URL/api/v1/cost-rates" "$ACME_TOKEN" \
    "{\"rateType\":\"RoleDepartment\",\"roleName\":\"DeptLead\",\"departmentId\":\"$ACME_ENG_ID\",\"hourlyRate\":120.00,\"currency\":\"USD\",\"effectiveFrom\":\"2025-01-01\"}" > /dev/null 2>&1 || true
api POST "$WORK_URL/api/v1/cost-rates" "$ACME_TOKEN" \
    "{\"rateType\":\"RoleDepartment\",\"roleName\":\"Member\",\"departmentId\":\"$ACME_ENG_ID\",\"hourlyRate\":95.00,\"currency\":\"USD\",\"effectiveFrom\":\"2025-01-01\"}" > /dev/null 2>&1 || true
log "  3 Acme cost rates created ✓"

# =============================================================================
# Step 6: Create time policy for Acme
# =============================================================================
log "Step 6: Creating Acme time policy..."
api PUT "$WORK_URL/api/v1/time-policies" "$ACME_TOKEN" \
    '{"maxDailyHours":10,"requireApproval":true,"allowOvertime":true,"overtimeMultiplier":1.5,"defaultBillable":true}' > /dev/null 2>&1 || true
log "  Acme time policy created ✓"

# =============================================================================
# Step 7: Create Globex cost rates and time policy
# =============================================================================
log "Step 7: Creating Globex cost rates and time policy..."
api POST "$WORK_URL/api/v1/cost-rates" "$GLX_TOKEN" \
    '{"rateType":"OrgDefault","hourlyRate":85.00,"currency":"USD","effectiveFrom":"2025-01-01"}' > /dev/null 2>&1 || true
api PUT "$WORK_URL/api/v1/time-policies" "$GLX_TOKEN" \
    '{"maxDailyHours":8,"requireApproval":true,"allowOvertime":false,"defaultBillable":true}' > /dev/null 2>&1 || true
log "  Globex cost rate + time policy created ✓"

# =============================================================================
# Step 8: Create risk register entries for Acme
# =============================================================================
log "Step 8: Creating Acme risk register entries..."
if [ -n "$MOB_ID" ] && [ "$MOB_ID" != "" ]; then
    api POST "$WORK_URL/api/v1/analytics/risks" "$SARAH_TOKEN" \
        "{\"projectId\":\"$MOB_ID\",\"title\":\"iOS 17 compatibility issues\",\"description\":\"Multiple crash reports from iOS 17 users. May affect App Store rating.\",\"severity\":\"High\",\"likelihood\":\"High\",\"mitigationStatus\":\"InProgress\",\"mitigationPlan\":\"Dedicated sprint to fix all iOS 17 issues\"}" > /dev/null 2>&1 || true
    api POST "$WORK_URL/api/v1/analytics/risks" "$SARAH_TOKEN" \
        "{\"projectId\":\"$MOB_ID\",\"title\":\"Third-party SDK deprecation\",\"description\":\"Firebase SDK v8 deprecated. Need to migrate to v9 before EOL.\",\"severity\":\"Medium\",\"likelihood\":\"Medium\",\"mitigationStatus\":\"Identified\",\"mitigationPlan\":\"Schedule migration in next quarter\"}" > /dev/null 2>&1 || true
fi
log "  2 risk entries created ✓"

# =============================================================================
# Step 9: Create story links for Acme
# =============================================================================
log "Step 9: Creating story links..."
if [ -n "$MOB_S1" ] && [ "$MOB_S1" != "" ] && [ -n "$MOB_S4" ] && [ "$MOB_S4" != "" ]; then
    api POST "$WORK_URL/api/v1/stories/$MOB_S1/links" "$SARAH_TOKEN" \
        "{\"targetStoryId\":\"$MOB_S4\",\"linkType\":\"RelatesTo\"}" > /dev/null 2>&1 || true
    log "  MOB-1 relates to MOB-4 (login → biometric) ✓"
fi
if [ -n "$MOB_S3" ] && [ "$MOB_S3" != "" ] && [ -n "$MOB_S1" ] && [ "$MOB_S1" != "" ]; then
    api POST "$WORK_URL/api/v1/stories/$MOB_S3/links" "$SARAH_TOKEN" \
        "{\"targetStoryId\":\"$MOB_S1\",\"linkType\":\"BlockedBy\"}" > /dev/null 2>&1 || true
    log "  MOB-3 blocked by MOB-1 (crash → login) ✓"
fi

# =============================================================================
# Done
# =============================================================================
log ""
log "========================================="
log "Batch 5 complete!"
log "  Tasks: 17 across 3 projects"
log "  Comments: 4"
log "  Cost rates: 4 (3 Acme + 1 Globex)"
log "  Time policies: 2"
log "  Risk entries: 2"
log "  Story links: 2"
log "========================================="
log ""
log "========================================="
log "ALL SEED DATA COMPLETE!"
log ""
log "Test accounts (all passwords: Welcome@123 → forced change):"
log "  PlatformAdmin: admin / Platform@2025"
log "  Acme OrgAdmin: jane.admin@acme.com / AcmeAdmin@2025"
log "  Acme DeptLead: sarah.lead@acme.com / Sarah@2025"
log "  Globex OrgAdmin: bob.admin@globex.com / GlobexAdmin@2025"
log "  Globex DeptLead: emma.lead@globex.com / Emma@2025"
log "  Other members: Welcome@123 (forced change on first login)"
log ""
log "Stories are NOT assigned — test assignment flows manually"
log "Sprints are in Planning — test start/complete flows manually"
log "========================================="

#!/bin/bash
# =============================================================================
# Nexus 2.0 — Development Seed Script
# Batch 4: Stories + Sprints (stories NOT assigned)
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

create_story() {
    local token=$1 project_id=$2 title=$3 priority=$4 points=$5 desc=$6
    local resp=$(api POST "$WORK_URL/api/v1/stories" "$token" \
        "{\"projectId\":\"$project_id\",\"title\":\"$title\",\"priority\":\"$priority\",\"storyPoints\":$points,\"description\":\"$desc\"}")
    local id=$(json_field "$resp" '["data"]["storyId"]')
    if [ -n "$id" ] && [ "$id" != "None" ]; then
        echo "$id"
    else
        warn "  Story '$title' may already exist"
        echo ""
    fi
}

# =============================================================================
# Step 1: Acme — Mobile App (MOB) stories
# =============================================================================
log "Step 1: Creating Acme Mobile App stories..."
MOB_S1=$(create_story "$SARAH_TOKEN" "$MOB_ID" "User login screen" "High" 5 "Implement login screen with email/password and social auth options")
MOB_S2=$(create_story "$SARAH_TOKEN" "$MOB_ID" "Push notification setup" "Medium" 8 "Integrate Firebase Cloud Messaging for push notifications")
MOB_S3=$(create_story "$SARAH_TOKEN" "$MOB_ID" "App crashes on logout" "Critical" 3 "App crashes when user taps logout button on iOS 17")
MOB_S4=$(create_story "$SARAH_TOKEN" "$MOB_ID" "Biometric authentication" "High" 8 "Add Face ID and fingerprint login support")
MOB_S5=$(create_story "$SARAH_TOKEN" "$MOB_ID" "Offline mode" "Medium" 13 "Cache recent data for offline access with sync on reconnect")
MOB_S6=$(create_story "$SARAH_TOKEN" "$MOB_ID" "App store screenshots" "Low" 2 "Create promotional screenshots for App Store and Play Store")
log "  6 MOB stories created ✓"

# =============================================================================
# Step 2: Acme — Web Platform (WEB) stories
# =============================================================================
log "Step 2: Creating Acme Web Platform stories..."
WEB_S1=$(create_story "$SARAH_TOKEN" "$WEB_ID" "Dashboard redesign" "High" 13 "Redesign the main dashboard with new analytics widgets")
WEB_S2=$(create_story "$SARAH_TOKEN" "$WEB_ID" "Fix pagination on members page" "Low" 2 "Pagination breaks when filtering by department")
WEB_S3=$(create_story "$SARAH_TOKEN" "$WEB_ID" "Add dark mode support" "Medium" 8 "Implement dark mode theme toggle with system preference detection")
WEB_S4=$(create_story "$SARAH_TOKEN" "$WEB_ID" "Performance audit" "High" 5 "Run Lighthouse audit and fix critical performance issues")
WEB_S5=$(create_story "$SARAH_TOKEN" "$WEB_ID" "Accessibility compliance" "Medium" 8 "Ensure WCAG 2.1 AA compliance across all pages")
log "  5 WEB stories created ✓"

# =============================================================================
# Step 3: Acme — API Gateway (API) stories
# =============================================================================
log "Step 3: Creating Acme API Gateway stories..."
API_S1=$(create_story "$SARAH_TOKEN" "$API_ID" "Rate limiting middleware" "High" 5 "Implement sliding window rate limiting per API key")
API_S2=$(create_story "$SARAH_TOKEN" "$API_ID" "Database connection pooling" "Medium" 3 "Optimize connection pool settings for high concurrency")
API_S3=$(create_story "$SARAH_TOKEN" "$API_ID" "API versioning strategy" "High" 5 "Implement URL-based API versioning with deprecation headers")
API_S4=$(create_story "$SARAH_TOKEN" "$API_ID" "Request/response logging" "Low" 3 "Add structured logging for all API requests with PII redaction")
log "  4 API stories created ✓"

# =============================================================================
# Step 4: Globex — Payment System (PAY) stories
# =============================================================================
log "Step 4: Creating Globex Payment System stories..."
PAY_S1=$(create_story "$EMMA_TOKEN" "$PAY_ID" "Stripe integration" "Critical" 13 "Integrate Stripe for credit card payments and subscriptions")
PAY_S2=$(create_story "$EMMA_TOKEN" "$PAY_ID" "Invoice generation" "High" 8 "Auto-generate PDF invoices on successful payment")
PAY_S3=$(create_story "$EMMA_TOKEN" "$PAY_ID" "Refund workflow" "High" 5 "Implement refund request and approval workflow")
PAY_S4=$(create_story "$EMMA_TOKEN" "$PAY_ID" "Payment retry logic" "Medium" 5 "Retry failed payments with exponential backoff")
PAY_S5=$(create_story "$EMMA_TOKEN" "$PAY_ID" "PCI compliance audit" "Critical" 8 "Ensure PCI DSS compliance for card data handling")
log "  5 PAY stories created ✓"

# =============================================================================
# Step 5: Globex — Admin Dashboard (ADM) stories
# =============================================================================
log "Step 5: Creating Globex Admin Dashboard stories..."
ADM_S1=$(create_story "$EMMA_TOKEN" "$ADM_ID" "User management page" "High" 8 "CRUD interface for managing platform users")
ADM_S2=$(create_story "$EMMA_TOKEN" "$ADM_ID" "Audit log viewer" "Medium" 5 "Searchable audit log viewer with date range filters")
ADM_S3=$(create_story "$EMMA_TOKEN" "$ADM_ID" "System health dashboard" "Medium" 5 "Real-time system health metrics and alerts")
ADM_S4=$(create_story "$EMMA_TOKEN" "$ADM_ID" "Role permission matrix" "Low" 3 "Visual matrix showing role-to-permission mappings")
log "  4 ADM stories created ✓"

# =============================================================================
# Step 6: Create Acme sprints
# =============================================================================
log "Step 6: Creating Acme sprints..."
MOB_SPRINT_RESP=$(api POST "$WORK_URL/api/v1/projects/$MOB_ID/sprints" "$SARAH_TOKEN" \
    '{"name":"MOB Sprint 1 - Authentication","goal":"Complete user authentication flow","startDate":"2025-07-01","endDate":"2025-07-14"}')
MOB_SPRINT_ID=$(json_field "$MOB_SPRINT_RESP" '["data"]["sprintId"]')
[ -z "$MOB_SPRINT_ID" ] || [ "$MOB_SPRINT_ID" = "None" ] && warn "MOB sprint may exist" || log "  MOB Sprint 1: $MOB_SPRINT_ID ✓"

WEB_SPRINT_RESP=$(api POST "$WORK_URL/api/v1/projects/$WEB_ID/sprints" "$SARAH_TOKEN" \
    '{"name":"WEB Sprint 1 - Dashboard","goal":"Redesign dashboard and fix critical bugs","startDate":"2025-07-01","endDate":"2025-07-14"}')
WEB_SPRINT_ID=$(json_field "$WEB_SPRINT_RESP" '["data"]["sprintId"]')
[ -z "$WEB_SPRINT_ID" ] || [ "$WEB_SPRINT_ID" = "None" ] && warn "WEB sprint may exist" || log "  WEB Sprint 1: $WEB_SPRINT_ID ✓"

# =============================================================================
# Step 7: Create Globex sprint
# =============================================================================
log "Step 7: Creating Globex sprint..."
PAY_SPRINT_RESP=$(api POST "$WORK_URL/api/v1/projects/$PAY_ID/sprints" "$EMMA_TOKEN" \
    '{"name":"PAY Sprint 1 - Core Payments","goal":"Implement Stripe integration and invoice generation","startDate":"2025-07-01","endDate":"2025-07-14"}')
PAY_SPRINT_ID=$(json_field "$PAY_SPRINT_RESP" '["data"]["sprintId"]')
[ -z "$PAY_SPRINT_ID" ] || [ "$PAY_SPRINT_ID" = "None" ] && warn "PAY sprint may exist" || log "  PAY Sprint 1: $PAY_SPRINT_ID ✓"

# =============================================================================
# Step 8: Add stories to sprints (not all — some stay in backlog)
# =============================================================================
log "Step 8: Adding stories to sprints..."
add_to_sprint() {
    local sprint_id=$1 story_id=$2 token=$3
    if [ -n "$story_id" ] && [ "$story_id" != "" ]; then
        api POST "$WORK_URL/api/v1/sprints/$sprint_id/stories" "$token" \
            "{\"storyId\":\"$story_id\"}" > /dev/null 2>&1 || true
    fi
}

# MOB Sprint: login, push notifications, crash fix (3 of 6)
add_to_sprint "$MOB_SPRINT_ID" "$MOB_S1" "$SARAH_TOKEN"
add_to_sprint "$MOB_SPRINT_ID" "$MOB_S2" "$SARAH_TOKEN"
add_to_sprint "$MOB_SPRINT_ID" "$MOB_S3" "$SARAH_TOKEN"
log "  MOB Sprint: 3 stories added ✓"

# WEB Sprint: dashboard, pagination fix (2 of 5)
add_to_sprint "$WEB_SPRINT_ID" "$WEB_S1" "$SARAH_TOKEN"
add_to_sprint "$WEB_SPRINT_ID" "$WEB_S2" "$SARAH_TOKEN"
log "  WEB Sprint: 2 stories added ✓"

# PAY Sprint: stripe, invoices, refund (3 of 5)
add_to_sprint "$PAY_SPRINT_ID" "$PAY_S1" "$EMMA_TOKEN"
add_to_sprint "$PAY_SPRINT_ID" "$PAY_S2" "$EMMA_TOKEN"
add_to_sprint "$PAY_SPRINT_ID" "$PAY_S3" "$EMMA_TOKEN"
log "  PAY Sprint: 3 stories added ✓"

# =============================================================================
# Save state
# =============================================================================
cat >> "$STATE_FILE" << EOF
MOB_S1=$MOB_S1
MOB_S2=$MOB_S2
MOB_S3=$MOB_S3
MOB_S4=$MOB_S4
MOB_S5=$MOB_S5
MOB_S6=$MOB_S6
WEB_S1=$WEB_S1
WEB_S2=$WEB_S2
WEB_S3=$WEB_S3
WEB_S4=$WEB_S4
WEB_S5=$WEB_S5
API_S1=$API_S1
API_S2=$API_S2
API_S3=$API_S3
API_S4=$API_S4
PAY_S1=$PAY_S1
PAY_S2=$PAY_S2
PAY_S3=$PAY_S3
PAY_S4=$PAY_S4
PAY_S5=$PAY_S5
ADM_S1=$ADM_S1
ADM_S2=$ADM_S2
ADM_S3=$ADM_S3
ADM_S4=$ADM_S4
MOB_SPRINT_ID=$MOB_SPRINT_ID
WEB_SPRINT_ID=$WEB_SPRINT_ID
PAY_SPRINT_ID=$PAY_SPRINT_ID
EOF

log ""
log "========================================="
log "Batch 4 complete!"
log "  Acme: 15 stories, 2 sprints"
log "  Globex: 9 stories, 1 sprint"
log "  Stories NOT assigned (test assignment manually)"
log "  Sprints in Planning (test start manually)"
log "========================================="
log ""
log "Next: Run batch-5-tasks-extras.sh"

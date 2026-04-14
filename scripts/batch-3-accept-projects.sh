#!/bin/bash
# =============================================================================
# Nexus 2.0 — Development Seed Script
# Batch 3: Accept Invites + Create Projects + Labels
# =============================================================================
set -e

SECURITY_URL="http://localhost:5001"
PROFILE_URL="http://localhost:5002"
WORK_URL="http://localhost:5003"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
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
api_public() { curl -s -X "$1" "$2" -H "Content-Type: application/json" -d "$3"; }

STATE_FILE="$(dirname "$0")/.seed-state.env"
[ -f "$STATE_FILE" ] || fail "Run batch-1 and batch-2 first."
source "$STATE_FILE"

# =============================================================================
# Step 1: Accept Acme invites
# =============================================================================
log "Step 1: Accepting Acme invites..."
accept_invite() {
    local token=$1 name=$2
    if [ -n "$token" ] && [ "$token" != "" ]; then
        api_public POST "$PROFILE_URL/api/v1/invites/$token/accept" '{}' > /dev/null 2>&1
        log "  $name accepted ✓"
    else
        warn "  $name — no invite token (may already be accepted)"
    fi
}

accept_invite "$ACME_INVITE_1" "sarah.lead@acme.com"
accept_invite "$ACME_INVITE_2" "mike.dev@acme.com"
accept_invite "$ACME_INVITE_3" "lisa.qa@acme.com"
accept_invite "$ACME_INVITE_4" "tom.viewer@acme.com"
accept_invite "$ACME_INVITE_5" "anna.devops@acme.com"
accept_invite "$ACME_INVITE_6" "chris.design@acme.com"

# =============================================================================
# Step 2: Accept Globex invites
# =============================================================================
log "Step 2: Accepting Globex invites..."
accept_invite "$GLX_INVITE_1" "emma.lead@globex.com"
accept_invite "$GLX_INVITE_2" "james.dev@globex.com"
accept_invite "$GLX_INVITE_3" "nina.qa@globex.com"
accept_invite "$GLX_INVITE_4" "alex.viewer@globex.com"

# =============================================================================
# Step 3: Login as Acme DeptLead to create projects
# =============================================================================
log "Step 3: Logging in as Acme DeptLead (sarah.lead@acme.com)..."
SARAH_LOGIN=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"sarah.lead@acme.com","password":"Welcome@123"}')
SARAH_TOKEN=$(json_field "$SARAH_LOGIN" '["data"]["accessToken"]')

if [ -z "$SARAH_TOKEN" ] || [ "$SARAH_TOKEN" = "None" ]; then
    fail "Could not login as sarah.lead@acme.com"
fi

IS_FIRST=$(json_field "$SARAH_LOGIN" '["data"]["isFirstTimeUser"]')
if [ "$IS_FIRST" = "True" ] || [ "$IS_FIRST" = "true" ]; then
    log "Changing Sarah's password..."
    api POST "$SECURITY_URL/api/v1/password/forced-change" "$SARAH_TOKEN" '{"newPassword":"Sarah@2025"}' > /dev/null
    SARAH_LOGIN=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"sarah.lead@acme.com","password":"Sarah@2025"}')
    SARAH_TOKEN=$(json_field "$SARAH_LOGIN" '["data"]["accessToken"]')
fi
log "Sarah logged in ✓"

# =============================================================================
# Step 4: Create Acme projects
# =============================================================================
log "Step 4: Creating Acme projects..."
MOB_RESP=$(api POST "$WORK_URL/api/v1/projects" "$SARAH_TOKEN" \
    '{"name":"Mobile App","projectKey":"MOB","description":"iOS and Android mobile application"}')
MOB_ID=$(json_field "$MOB_RESP" '["data"]["projectId"]')
[ -z "$MOB_ID" ] || [ "$MOB_ID" = "None" ] && warn "MOB project may already exist" || log "  Mobile App (MOB): $MOB_ID ✓"

WEB_RESP=$(api POST "$WORK_URL/api/v1/projects" "$SARAH_TOKEN" \
    '{"name":"Web Platform","projectKey":"WEB","description":"Customer-facing web application"}')
WEB_ID=$(json_field "$WEB_RESP" '["data"]["projectId"]')
[ -z "$WEB_ID" ] || [ "$WEB_ID" = "None" ] && warn "WEB project may already exist" || log "  Web Platform (WEB): $WEB_ID ✓"

API_RESP=$(api POST "$WORK_URL/api/v1/projects" "$SARAH_TOKEN" \
    '{"name":"API Gateway","projectKey":"API","description":"Backend API infrastructure"}')
API_ID=$(json_field "$API_RESP" '["data"]["projectId"]')
[ -z "$API_ID" ] || [ "$API_ID" = "None" ] && warn "API project may already exist" || log "  API Gateway (API): $API_ID ✓"

# =============================================================================
# Step 5: Login as Globex DeptLead to create projects
# =============================================================================
log "Step 5: Logging in as Globex DeptLead (emma.lead@globex.com)..."
EMMA_LOGIN=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"emma.lead@globex.com","password":"Welcome@123"}')
EMMA_TOKEN=$(json_field "$EMMA_LOGIN" '["data"]["accessToken"]')

if [ -z "$EMMA_TOKEN" ] || [ "$EMMA_TOKEN" = "None" ]; then
    fail "Could not login as emma.lead@globex.com"
fi

IS_FIRST=$(json_field "$EMMA_LOGIN" '["data"]["isFirstTimeUser"]')
if [ "$IS_FIRST" = "True" ] || [ "$IS_FIRST" = "true" ]; then
    log "Changing Emma's password..."
    api POST "$SECURITY_URL/api/v1/password/forced-change" "$EMMA_TOKEN" '{"newPassword":"Emma@2025"}' > /dev/null
    EMMA_LOGIN=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"emma.lead@globex.com","password":"Emma@2025"}')
    EMMA_TOKEN=$(json_field "$EMMA_LOGIN" '["data"]["accessToken"]')
fi
log "Emma logged in ✓"

# =============================================================================
# Step 6: Create Globex projects
# =============================================================================
log "Step 6: Creating Globex projects..."
PAY_RESP=$(api POST "$WORK_URL/api/v1/projects" "$EMMA_TOKEN" \
    '{"name":"Payment System","projectKey":"PAY","description":"Payment processing and billing system"}')
PAY_ID=$(json_field "$PAY_RESP" '["data"]["projectId"]')
[ -z "$PAY_ID" ] || [ "$PAY_ID" = "None" ] && warn "PAY project may already exist" || log "  Payment System (PAY): $PAY_ID ✓"

ADM_RESP=$(api POST "$WORK_URL/api/v1/projects" "$EMMA_TOKEN" \
    '{"name":"Admin Dashboard","projectKey":"ADM","description":"Internal admin management dashboard"}')
ADM_ID=$(json_field "$ADM_RESP" '["data"]["projectId"]')
[ -z "$ADM_ID" ] || [ "$ADM_ID" = "None" ] && warn "ADM project may already exist" || log "  Admin Dashboard (ADM): $ADM_ID ✓"

# =============================================================================
# Step 7: Create Acme labels
# =============================================================================
log "Step 7: Creating Acme labels..."
for label in '{"name":"Bug","color":"#DC2626"}' '{"name":"Feature","color":"#2563EB"}' '{"name":"Tech Debt","color":"#9333EA"}' '{"name":"UX","color":"#EC4899"}' '{"name":"Performance","color":"#F59E0B"}'; do
    api POST "$WORK_URL/api/v1/labels" "$SARAH_TOKEN" "$label" > /dev/null 2>&1 || true
done
log "Acme labels created ✓"

# =============================================================================
# Step 8: Create Globex labels
# =============================================================================
log "Step 8: Creating Globex labels..."
for label in '{"name":"Bug","color":"#DC2626"}' '{"name":"Feature","color":"#2563EB"}' '{"name":"Security","color":"#EF4444"}' '{"name":"Compliance","color":"#6366F1"}'; do
    api POST "$WORK_URL/api/v1/labels" "$EMMA_TOKEN" "$label" > /dev/null 2>&1 || true
done
log "Globex labels created ✓"

# =============================================================================
# Save state
# =============================================================================
cat >> "$STATE_FILE" << EOF
SARAH_TOKEN=$SARAH_TOKEN
EMMA_TOKEN=$EMMA_TOKEN
MOB_ID=$MOB_ID
WEB_ID=$WEB_ID
API_ID=$API_ID
PAY_ID=$PAY_ID
ADM_ID=$ADM_ID
EOF

log ""
log "========================================="
log "Batch 3 complete!"
log "  Acme: 3 projects + 5 labels"
log "  Globex: 2 projects + 4 labels"
log "========================================="
log ""
log "Next: Run batch-4-stories-sprints.sh"

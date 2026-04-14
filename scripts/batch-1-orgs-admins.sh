#!/bin/bash
# =============================================================================
# Nexus 2.0 — Development Seed Script
# Batch 1: PlatformAdmin + Organizations + OrgAdmins
# =============================================================================
set -e

SECURITY_URL="http://localhost:5001"
PROFILE_URL="http://localhost:5002"
WORK_URL="http://localhost:5003"
UTILITY_URL="http://localhost:5200"
BILLING_URL="http://localhost:5300"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log() { echo -e "${GREEN}[SEED]${NC} $1"; }
warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
fail() { echo -e "${RED}[FAIL]${NC} $1"; exit 1; }

# Helper: extract JSON field
json_field() { echo "$1" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d$2)" 2>/dev/null; }

# Helper: API call with token
api() {
    local method=$1 url=$2 token=$3 body=$4
    if [ -n "$body" ]; then
        curl -s -X "$method" "$url" \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer $token" \
            -d "$body"
    else
        curl -s -X "$method" "$url" \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer $token"
    fi
}

# Helper: API call without token
api_public() {
    local method=$1 url=$2 body=$3
    curl -s -X "$method" "$url" \
        -H "Content-Type: application/json" \
        -d "$body"
}

# =============================================================================
# Step 1: Login as PlatformAdmin
# =============================================================================
log "Step 1: Logging in as PlatformAdmin..."
LOGIN_RESP=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"admin","password":"Admin@123"}')
ADMIN_TOKEN=$(json_field "$LOGIN_RESP" '["data"]["accessToken"]')

if [ -z "$ADMIN_TOKEN" ] || [ "$ADMIN_TOKEN" = "None" ]; then
    warn "PlatformAdmin may already have changed password. Trying Platform@2025..."
    LOGIN_RESP=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"admin","password":"Platform@2025"}')
    ADMIN_TOKEN=$(json_field "$LOGIN_RESP" '["data"]["accessToken"]')
fi

if [ -z "$ADMIN_TOKEN" ] || [ "$ADMIN_TOKEN" = "None" ]; then
    fail "Could not login as PlatformAdmin. Is SecurityService running?"
fi
log "PlatformAdmin logged in ✓"

# Check if first-time user
IS_FIRST=$(json_field "$LOGIN_RESP" '["data"]["isFirstTimeUser"]')
if [ "$IS_FIRST" = "True" ] || [ "$IS_FIRST" = "true" ]; then
    log "Changing PlatformAdmin password (first-time)..."
    api POST "$SECURITY_URL/api/v1/password/forced-change" "$ADMIN_TOKEN" '{"newPassword":"Platform@2025"}' > /dev/null
    
    # Re-login with new password
    LOGIN_RESP=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"admin","password":"Platform@2025"}')
    ADMIN_TOKEN=$(json_field "$LOGIN_RESP" '["data"]["accessToken"]')
    log "Password changed and re-logged in ✓"
fi

# =============================================================================
# Step 2: Create Organization — Acme Corp
# =============================================================================
log "Step 2: Creating Acme Corp..."
ACME_RESP=$(api POST "$PROFILE_URL/api/v1/organizations" "$ADMIN_TOKEN" \
    '{"name":"Acme Corp","storyIdPrefix":"ACME"}')
ACME_ORG_ID=$(json_field "$ACME_RESP" '["data"]["organizationId"]')

if [ -z "$ACME_ORG_ID" ] || [ "$ACME_ORG_ID" = "None" ]; then
    warn "Acme Corp may already exist. Checking..."
    ORGS_RESP=$(api GET "$PROFILE_URL/api/v1/organizations" "$ADMIN_TOKEN")
    ACME_ORG_ID=$(echo "$ORGS_RESP" | python3 -c "
import sys,json
d=json.load(sys.stdin)
items = d.get('data',{}).get('data',[])
for o in items:
    if o.get('organizationName')=='Acme Corp':
        print(o['organizationId']); break
" 2>/dev/null)
    if [ -z "$ACME_ORG_ID" ] || [ "$ACME_ORG_ID" = "None" ]; then
        fail "Could not create or find Acme Corp"
    fi
    log "Acme Corp already exists: $ACME_ORG_ID ✓"
else
    log "Acme Corp created: $ACME_ORG_ID ✓"
fi

# =============================================================================
# Step 3: Create Organization — Globex Inc
# =============================================================================
log "Step 3: Creating Globex Inc..."
GLX_RESP=$(api POST "$PROFILE_URL/api/v1/organizations" "$ADMIN_TOKEN" \
    '{"name":"Globex Inc","storyIdPrefix":"GLX"}')
GLX_ORG_ID=$(json_field "$GLX_RESP" '["data"]["organizationId"]')

if [ -z "$GLX_ORG_ID" ] || [ "$GLX_ORG_ID" = "None" ]; then
    warn "Globex Inc may already exist. Checking..."
    ORGS_RESP=$(api GET "$PROFILE_URL/api/v1/organizations" "$ADMIN_TOKEN")
    GLX_ORG_ID=$(echo "$ORGS_RESP" | python3 -c "
import sys,json
d=json.load(sys.stdin)
items = d.get('data',{}).get('data',[])
for o in items:
    if o.get('organizationName')=='Globex Inc':
        print(o['organizationId']); break
" 2>/dev/null)
    if [ -z "$GLX_ORG_ID" ] || [ "$GLX_ORG_ID" = "None" ]; then
        fail "Could not create or find Globex Inc"
    fi
    log "Globex Inc already exists: $GLX_ORG_ID ✓"
else
    log "Globex Inc created: $GLX_ORG_ID ✓"
fi

# =============================================================================
# Step 4: Provision OrgAdmin for Acme Corp
# =============================================================================
log "Step 4: Provisioning OrgAdmin for Acme Corp..."
ACME_ADMIN_RESP=$(api POST "$PROFILE_URL/api/v1/organizations/$ACME_ORG_ID/provision-admin" "$ADMIN_TOKEN" \
    '{"email":"jane.admin@acme.com","firstName":"Jane","lastName":"Admin"}')
ACME_ADMIN_ID=$(json_field "$ACME_ADMIN_RESP" '["data"]["teamMemberId"]')

if [ -z "$ACME_ADMIN_ID" ] || [ "$ACME_ADMIN_ID" = "None" ]; then
    warn "Acme OrgAdmin may already exist (email already registered)"
else
    log "Acme OrgAdmin provisioned: $ACME_ADMIN_ID ✓"
fi

# =============================================================================
# Step 5: Provision OrgAdmin for Globex Inc
# =============================================================================
log "Step 5: Provisioning OrgAdmin for Globex Inc..."
GLX_ADMIN_RESP=$(api POST "$PROFILE_URL/api/v1/organizations/$GLX_ORG_ID/provision-admin" "$ADMIN_TOKEN" \
    '{"email":"bob.admin@globex.com","firstName":"Bob","lastName":"Admin"}')
GLX_ADMIN_ID=$(json_field "$GLX_ADMIN_RESP" '["data"]["teamMemberId"]')

if [ -z "$GLX_ADMIN_ID" ] || [ "$GLX_ADMIN_ID" = "None" ]; then
    warn "Globex OrgAdmin may already exist (email already registered)"
else
    log "Globex OrgAdmin provisioned: $GLX_ADMIN_ID ✓"
fi

# =============================================================================
# Save state for next batch
# =============================================================================
STATE_FILE="$(dirname "$0")/.seed-state.env"
cat > "$STATE_FILE" << EOF
ADMIN_TOKEN=$ADMIN_TOKEN
ACME_ORG_ID=$ACME_ORG_ID
GLX_ORG_ID=$GLX_ORG_ID
ACME_ADMIN_ID=$ACME_ADMIN_ID
GLX_ADMIN_ID=$GLX_ADMIN_ID
EOF

log ""
log "========================================="
log "Batch 1 complete!"
log "  Acme Corp:  $ACME_ORG_ID"
log "  Globex Inc: $GLX_ORG_ID"
log "========================================="
log ""
log "Next: Run batch-2-subscriptions-members.sh"

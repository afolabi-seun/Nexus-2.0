#!/bin/bash
# =============================================================================
# Nexus 2.0 — Development Seed Script
# Batch 2: OrgAdmin Login + Subscriptions + Departments + Invite Members
# =============================================================================
set -e

SECURITY_URL="http://localhost:5001"
PROFILE_URL="http://localhost:5002"
BILLING_URL="http://localhost:5300"

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

# Load state from batch 1
STATE_FILE="$(dirname "$0")/.seed-state.env"
[ -f "$STATE_FILE" ] || fail "Run batch-1 first. State file not found."
source "$STATE_FILE"

# =============================================================================
# Step 1: Login as Acme OrgAdmin
# =============================================================================
log "Step 1: Logging in as Acme OrgAdmin (jane.admin@acme.com)..."
ACME_LOGIN=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"jane.admin@acme.com","password":"Welcome@123"}')
ACME_TOKEN=$(json_field "$ACME_LOGIN" '["data"]["accessToken"]')

if [ -z "$ACME_TOKEN" ] || [ "$ACME_TOKEN" = "None" ]; then
    warn "Trying with changed password..."
    ACME_LOGIN=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"jane.admin@acme.com","password":"AcmeAdmin@2025"}')
    ACME_TOKEN=$(json_field "$ACME_LOGIN" '["data"]["accessToken"]')
fi
[ -z "$ACME_TOKEN" ] || [ "$ACME_TOKEN" = "None" ] && fail "Could not login as Acme OrgAdmin"

IS_FIRST=$(json_field "$ACME_LOGIN" '["data"]["isFirstTimeUser"]')
if [ "$IS_FIRST" = "True" ] || [ "$IS_FIRST" = "true" ]; then
    log "Changing Acme OrgAdmin password..."
    api POST "$SECURITY_URL/api/v1/password/forced-change" "$ACME_TOKEN" '{"newPassword":"AcmeAdmin@2025"}' > /dev/null
    ACME_LOGIN=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"jane.admin@acme.com","password":"AcmeAdmin@2025"}')
    ACME_TOKEN=$(json_field "$ACME_LOGIN" '["data"]["accessToken"]')
fi
log "Acme OrgAdmin logged in ✓"

# =============================================================================
# Step 2: Login as Globex OrgAdmin
# =============================================================================
log "Step 2: Logging in as Globex OrgAdmin (bob.admin@globex.com)..."
GLX_LOGIN=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"bob.admin@globex.com","password":"Welcome@123"}')
GLX_TOKEN=$(json_field "$GLX_LOGIN" '["data"]["accessToken"]')

if [ -z "$GLX_TOKEN" ] || [ "$GLX_TOKEN" = "None" ]; then
    warn "Trying with changed password..."
    GLX_LOGIN=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"bob.admin@globex.com","password":"GlobexAdmin@2025"}')
    GLX_TOKEN=$(json_field "$GLX_LOGIN" '["data"]["accessToken"]')
fi
[ -z "$GLX_TOKEN" ] || [ "$GLX_TOKEN" = "None" ] && fail "Could not login as Globex OrgAdmin"

IS_FIRST=$(json_field "$GLX_LOGIN" '["data"]["isFirstTimeUser"]')
if [ "$IS_FIRST" = "True" ] || [ "$IS_FIRST" = "true" ]; then
    log "Changing Globex OrgAdmin password..."
    api POST "$SECURITY_URL/api/v1/password/forced-change" "$GLX_TOKEN" '{"newPassword":"GlobexAdmin@2025"}' > /dev/null
    GLX_LOGIN=$(api_public POST "$SECURITY_URL/api/v1/auth/login" '{"email":"bob.admin@globex.com","password":"GlobexAdmin@2025"}')
    GLX_TOKEN=$(json_field "$GLX_LOGIN" '["data"]["accessToken"]')
fi
log "Globex OrgAdmin logged in ✓"

# =============================================================================
# Step 3: Get plan IDs
# =============================================================================
log "Step 3: Fetching plans..."
PLANS_RESP=$(api GET "$BILLING_URL/api/v1/plans" "$ACME_TOKEN")
PROFESSIONAL_PLAN_ID=$(echo "$PLANS_RESP" | python3 -c "
import sys,json
d=json.load(sys.stdin)
for p in d.get('data',[]):
    if p.get('planCode')=='professional': print(p['planId']); break
" 2>/dev/null)
STARTER_PLAN_ID=$(echo "$PLANS_RESP" | python3 -c "
import sys,json
d=json.load(sys.stdin)
for p in d.get('data',[]):
    if p.get('planCode')=='starter': print(p['planId']); break
" 2>/dev/null)
log "Plans: Professional=$PROFESSIONAL_PLAN_ID, Starter=$STARTER_PLAN_ID ✓"

# =============================================================================
# Step 4: Create subscriptions
# =============================================================================
log "Step 4: Creating Acme subscription (Professional)..."
api POST "$BILLING_URL/api/v1/subscriptions" "$ACME_TOKEN" \
    "{\"planId\":\"$PROFESSIONAL_PLAN_ID\"}" > /dev/null 2>&1 || warn "Acme subscription may already exist"
log "Acme subscription created ✓"

log "Creating Globex subscription (Starter)..."
api POST "$BILLING_URL/api/v1/subscriptions" "$GLX_TOKEN" \
    "{\"planId\":\"$STARTER_PLAN_ID\"}" > /dev/null 2>&1 || warn "Globex subscription may already exist"
log "Globex subscription created ✓"

# =============================================================================
# Step 5: Create additional departments for Acme
# =============================================================================
log "Step 5: Creating additional departments for Acme..."
api POST "$PROFILE_URL/api/v1/departments" "$ACME_TOKEN" \
    '{"departmentName":"Mobile","departmentCode":"MOB"}' > /dev/null 2>&1 || warn "Mobile dept may exist"
api POST "$PROFILE_URL/api/v1/departments" "$ACME_TOKEN" \
    '{"departmentName":"Data","departmentCode":"DATA"}' > /dev/null 2>&1 || warn "Data dept may exist"
log "Acme departments created ✓"

log "Creating additional department for Globex..."
api POST "$PROFILE_URL/api/v1/departments" "$GLX_TOKEN" \
    '{"departmentName":"Security","departmentCode":"SEC"}' > /dev/null 2>&1 || warn "Security dept may exist"
log "Globex departments created ✓"

# =============================================================================
# Step 6: Get department IDs
# =============================================================================
log "Step 6: Fetching department IDs..."
ACME_DEPTS=$(api GET "$PROFILE_URL/api/v1/departments" "$ACME_TOKEN")
get_dept_id() {
    echo "$1" | python3 -c "
import sys,json
d=json.load(sys.stdin)
for dept in d.get('data',{}).get('data',[]):
    if dept.get('departmentCode')=='$2': print(dept['departmentId']); break
" 2>/dev/null
}
ACME_ENG_ID=$(get_dept_id "$ACME_DEPTS" "ENG")
ACME_QA_ID=$(get_dept_id "$ACME_DEPTS" "QA")
ACME_DEVOPS_ID=$(get_dept_id "$ACME_DEPTS" "DEVOPS")
ACME_PROD_ID=$(get_dept_id "$ACME_DEPTS" "PROD")
ACME_DESIGN_ID=$(get_dept_id "$ACME_DEPTS" "DESIGN")
log "Acme depts: ENG=$ACME_ENG_ID, QA=$ACME_QA_ID ✓"

GLX_DEPTS=$(api GET "$PROFILE_URL/api/v1/departments" "$GLX_TOKEN")
GLX_ENG_ID=$(get_dept_id "$GLX_DEPTS" "ENG")
GLX_QA_ID=$(get_dept_id "$GLX_DEPTS" "QA")
GLX_PROD_ID=$(get_dept_id "$GLX_DEPTS" "PROD")
log "Globex depts: ENG=$GLX_ENG_ID, QA=$GLX_QA_ID ✓"

# =============================================================================
# Step 7: Get role IDs
# =============================================================================
log "Step 7: Fetching role IDs..."
ROLES_RESP=$(api GET "$PROFILE_URL/api/v1/roles" "$ACME_TOKEN")
get_role_id() {
    echo "$ROLES_RESP" | python3 -c "
import sys,json
d=json.load(sys.stdin)
for r in d.get('data',[]):
    if r.get('roleName')=='$1': print(r['roleId']); break
" 2>/dev/null
}
DEPTLEAD_ROLE_ID=$(get_role_id "DeptLead")
MEMBER_ROLE_ID=$(get_role_id "Member")
VIEWER_ROLE_ID=$(get_role_id "Viewer")
log "Roles: DeptLead=$DEPTLEAD_ROLE_ID, Member=$MEMBER_ROLE_ID, Viewer=$VIEWER_ROLE_ID ✓"

# =============================================================================
# Step 8: Invite Acme team members
# =============================================================================
log "Step 8: Inviting Acme team members..."
invite_member() {
    local token=$1 email=$2 first=$3 last=$4 dept_id=$5 role_id=$6
    local resp=$(api POST "$PROFILE_URL/api/v1/invites" "$token" \
        "{\"email\":\"$email\",\"firstName\":\"$first\",\"lastName\":\"$last\",\"departmentId\":\"$dept_id\",\"roleId\":\"$role_id\"}")
    local invite_token=$(json_field "$resp" '["data"]["token"]')
    if [ -n "$invite_token" ] && [ "$invite_token" != "None" ]; then
        echo "$invite_token"
    else
        warn "Invite for $email may already exist"
        echo ""
    fi
}

ACME_INVITE_1=$(invite_member "$ACME_TOKEN" "sarah.lead@acme.com" "Sarah" "Lead" "$ACME_ENG_ID" "$DEPTLEAD_ROLE_ID")
ACME_INVITE_2=$(invite_member "$ACME_TOKEN" "mike.dev@acme.com" "Mike" "Developer" "$ACME_ENG_ID" "$MEMBER_ROLE_ID")
ACME_INVITE_3=$(invite_member "$ACME_TOKEN" "lisa.qa@acme.com" "Lisa" "Tester" "$ACME_QA_ID" "$MEMBER_ROLE_ID")
ACME_INVITE_4=$(invite_member "$ACME_TOKEN" "tom.viewer@acme.com" "Tom" "Viewer" "$ACME_PROD_ID" "$VIEWER_ROLE_ID")
ACME_INVITE_5=$(invite_member "$ACME_TOKEN" "anna.devops@acme.com" "Anna" "DevOps" "$ACME_DEVOPS_ID" "$DEPTLEAD_ROLE_ID")
ACME_INVITE_6=$(invite_member "$ACME_TOKEN" "chris.design@acme.com" "Chris" "Designer" "$ACME_DESIGN_ID" "$MEMBER_ROLE_ID")
log "Acme invites sent ✓"

# =============================================================================
# Step 9: Invite Globex team members
# =============================================================================
log "Step 9: Inviting Globex team members..."
GLX_INVITE_1=$(invite_member "$GLX_TOKEN" "emma.lead@globex.com" "Emma" "Lead" "$GLX_ENG_ID" "$DEPTLEAD_ROLE_ID")
GLX_INVITE_2=$(invite_member "$GLX_TOKEN" "james.dev@globex.com" "James" "Developer" "$GLX_ENG_ID" "$MEMBER_ROLE_ID")
GLX_INVITE_3=$(invite_member "$GLX_TOKEN" "nina.qa@globex.com" "Nina" "Tester" "$GLX_QA_ID" "$MEMBER_ROLE_ID")
GLX_INVITE_4=$(invite_member "$GLX_TOKEN" "alex.viewer@globex.com" "Alex" "Viewer" "$GLX_PROD_ID" "$VIEWER_ROLE_ID")
log "Globex invites sent ✓"

# =============================================================================
# Save state
# =============================================================================
cat >> "$STATE_FILE" << EOF
ACME_TOKEN=$ACME_TOKEN
GLX_TOKEN=$GLX_TOKEN
ACME_ENG_ID=$ACME_ENG_ID
ACME_QA_ID=$ACME_QA_ID
ACME_DEVOPS_ID=$ACME_DEVOPS_ID
ACME_PROD_ID=$ACME_PROD_ID
ACME_DESIGN_ID=$ACME_DESIGN_ID
GLX_ENG_ID=$GLX_ENG_ID
GLX_QA_ID=$GLX_QA_ID
GLX_PROD_ID=$GLX_PROD_ID
DEPTLEAD_ROLE_ID=$DEPTLEAD_ROLE_ID
MEMBER_ROLE_ID=$MEMBER_ROLE_ID
VIEWER_ROLE_ID=$VIEWER_ROLE_ID
ACME_INVITE_1=$ACME_INVITE_1
ACME_INVITE_2=$ACME_INVITE_2
ACME_INVITE_3=$ACME_INVITE_3
ACME_INVITE_4=$ACME_INVITE_4
ACME_INVITE_5=$ACME_INVITE_5
ACME_INVITE_6=$ACME_INVITE_6
GLX_INVITE_1=$GLX_INVITE_1
GLX_INVITE_2=$GLX_INVITE_2
GLX_INVITE_3=$GLX_INVITE_3
GLX_INVITE_4=$GLX_INVITE_4
EOF

log ""
log "========================================="
log "Batch 2 complete!"
log "  Acme: 6 members invited"
log "  Globex: 4 members invited"
log "========================================="
log ""
log "Next: Run batch-3-accept-projects.sh"

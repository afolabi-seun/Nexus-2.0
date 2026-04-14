#!/bin/bash
# =============================================================================
# Nexus 2.0 — Development Seed Script (All Batches)
# 
# Prerequisites:
#   - All 5 backend services running (ports 5001-5003, 5200, 5300)
#   - Databases created and migrated (auto on first startup)
#   - python3 available (for JSON parsing)
#
# Usage:
#   ./scripts/seed-all.sh
#
# This creates:
#   2 organizations (Acme Corp, Globex Inc)
#   2 OrgAdmins + 10 team members
#   5 projects with 24 stories
#   3 sprints (Planning status)
#   17 tasks, 4 comments, 4 cost rates
#   2 time policies, 2 risk entries, 2 story links
#   9 labels across both orgs
# =============================================================================
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Clean previous state
rm -f "$SCRIPT_DIR/.seed-state.env"

echo ""
echo "╔══════════════════════════════════════════╗"
echo "║   Nexus 2.0 — Development Seed Script   ║"
echo "╚══════════════════════════════════════════╝"
echo ""

# Check services are running
for port in 5001 5002 5003 5200 5300; do
    if ! curl -s "http://localhost:$port/health" > /dev/null 2>&1; then
        echo "ERROR: Service on port $port is not running."
        echo "Start all services first, then re-run this script."
        exit 1
    fi
done
echo "All services healthy ✓"
echo ""

bash "$SCRIPT_DIR/batch-1-orgs-admins.sh"
echo ""
bash "$SCRIPT_DIR/batch-2-subscriptions-members.sh"
echo ""
bash "$SCRIPT_DIR/batch-3-accept-projects.sh"
echo ""
bash "$SCRIPT_DIR/batch-4-stories-sprints.sh"
echo ""
bash "$SCRIPT_DIR/batch-5-tasks-extras.sh"

# Cleanup state file
rm -f "$SCRIPT_DIR/.seed-state.env"

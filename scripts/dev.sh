#!/bin/bash
# =============================================================================
# Nexus 2.0 — Start Services + Wait for Health + Seed
# =============================================================================
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'
log() { echo -e "${GREEN}[SETUP]${NC} $1"; }
warn() { echo -e "${YELLOW}[SETUP]${NC} $1"; }
fail() { echo -e "${RED}[SETUP]${NC} $1"; exit 1; }

SERVICES=(
    "SecurityService:5001"
    "ProfileService:5002"
    "WorkService:5003"
    "UtilityService:5200"
    "BillingService:5300"
)

PID_DIR="$SCRIPT_DIR/.pids"
mkdir -p "$PID_DIR"

# =============================================================================
# Start services
# =============================================================================
start_services() {
    log "Starting backend services..."
    for entry in "${SERVICES[@]}"; do
        name="${entry%%:*}"
        port="${entry##*:}"

        # Check if already running
        if curl -s "http://localhost:$port/health" > /dev/null 2>&1; then
            log "  $name already running on port $port ✓"
            continue
        fi

        log "  Starting $name on port $port..."
        dotnet run --project "$PROJECT_ROOT/src/backend/$name/$name.Api" \
            --no-launch-profile > "$PID_DIR/$name.log" 2>&1 &
        echo $! > "$PID_DIR/$name.pid"
    done
}

# =============================================================================
# Wait for all services to be healthy
# =============================================================================
wait_for_health() {
    local max_wait=120
    local interval=3
    local elapsed=0

    log "Waiting for services to be healthy (max ${max_wait}s)..."

    while [ $elapsed -lt $max_wait ]; do
        local all_healthy=true
        for entry in "${SERVICES[@]}"; do
            name="${entry%%:*}"
            port="${entry##*:}"
            if ! curl -s "http://localhost:$port/health" > /dev/null 2>&1; then
                all_healthy=false
                break
            fi
        done

        if $all_healthy; then
            log "All services healthy ✓"
            return 0
        fi

        sleep $interval
        elapsed=$((elapsed + interval))
        echo -ne "\r  Waiting... ${elapsed}s / ${max_wait}s"
    done

    echo ""
    fail "Services did not become healthy within ${max_wait}s"
    
    # Show which services failed
    for entry in "${SERVICES[@]}"; do
        name="${entry%%:*}"
        port="${entry##*:}"
        if ! curl -s "http://localhost:$port/health" > /dev/null 2>&1; then
            warn "  $name (port $port) — NOT healthy"
            if [ -f "$PID_DIR/$name.log" ]; then
                tail -5 "$PID_DIR/$name.log"
            fi
        fi
    done
    exit 1
}

# =============================================================================
# Stop services
# =============================================================================
stop_services() {
    log "Stopping backend services..."
    for entry in "${SERVICES[@]}"; do
        name="${entry%%:*}"
        pid_file="$PID_DIR/$name.pid"
        if [ -f "$pid_file" ]; then
            pid=$(cat "$pid_file")
            if kill -0 "$pid" 2>/dev/null; then
                kill "$pid" 2>/dev/null || true
                log "  $name (PID $pid) stopped ✓"
            fi
            rm -f "$pid_file"
        fi
    done
}

# =============================================================================
# Create databases
# =============================================================================
create_databases() {
    log "Creating databases (if they don't exist)..."
    for db in nexus_security nexus_profile nexus_work nexus_utility nexus_billing; do
        if psql -lqt 2>/dev/null | cut -d \| -f 1 | grep -qw "$db"; then
            log "  $db already exists ✓"
        else
            createdb "$db" 2>/dev/null && log "  $db created ✓" || warn "  $db — could not create (check PostgreSQL)"
        fi
    done
}

# =============================================================================
# Drop databases
# =============================================================================
drop_databases() {
    log "Dropping databases..."
    for db in nexus_security nexus_profile nexus_work nexus_utility nexus_billing; do
        dropdb --if-exists "$db" 2>/dev/null && log "  $db dropped ✓" || warn "  $db — could not drop"
    done
}

# =============================================================================
# Run seed
# =============================================================================
run_seed() {
    log "Running seed scripts..."
    bash "$SCRIPT_DIR/seed-all.sh"
}

# =============================================================================
# Main
# =============================================================================
case "${1:-}" in
    start)
        start_services
        wait_for_health
        ;;
    stop)
        stop_services
        ;;
    seed)
        wait_for_health
        run_seed
        ;;
    setup)
        create_databases
        start_services
        wait_for_health
        run_seed
        ;;
    reset)
        stop_services
        sleep 2
        drop_databases
        create_databases
        start_services
        wait_for_health
        run_seed
        ;;
    create-dbs)
        create_databases
        ;;
    status)
        for entry in "${SERVICES[@]}"; do
            name="${entry%%:*}"
            port="${entry##*:}"
            if curl -s "http://localhost:$port/health" > /dev/null 2>&1; then
                echo -e "  ${GREEN}●${NC} $name (port $port) — healthy"
            else
                echo -e "  ${RED}●${NC} $name (port $port) — not running"
            fi
        done
        ;;
    *)
        echo "Usage: $0 {start|stop|seed|setup|reset|create-dbs|status}"
        echo ""
        echo "Commands:"
        echo "  start      Start all backend services"
        echo "  stop       Stop all backend services"
        echo "  seed       Run seed scripts (services must be running)"
        echo "  setup      Create DBs + start services + seed"
        echo "  reset      Stop + drop DBs + recreate + start + seed"
        echo "  create-dbs Create databases only"
        echo "  status     Check service health"
        exit 1
        ;;
esac

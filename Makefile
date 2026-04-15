# =============================================================================
# Nexus 2.0 — Makefile
# =============================================================================

.PHONY: setup start stop seed reset status test test-backend test-frontend build clean create-dbs help

# Default target
help: ## Show this help
	@echo ""
	@echo "Nexus 2.0 — Development Commands"
	@echo "================================="
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2}'
	@echo ""

# ---------------------------------------------------------------------------
# Service Management
# ---------------------------------------------------------------------------

setup: ## Create DBs + start services + wait + seed (fresh machine)
	@./scripts/dev.sh setup

start: ## Start all backend services
	@./scripts/dev.sh start

stop: ## Stop all backend services
	@./scripts/dev.sh stop

status: ## Check service health
	@./scripts/dev.sh status

seed: ## Run seed scripts (services must be running)
	@./scripts/dev.sh seed

reset: ## Stop + drop DBs + recreate + start + seed (full reset)
	@./scripts/dev.sh reset

create-dbs: ## Create databases only
	@./scripts/dev.sh create-dbs

# ---------------------------------------------------------------------------
# Frontend
# ---------------------------------------------------------------------------

frontend: ## Start frontend dev server
	cd src/frontend && npm run dev

frontend-install: ## Install frontend dependencies
	cd src/frontend && npm install

# ---------------------------------------------------------------------------
# Testing
# ---------------------------------------------------------------------------

test: test-backend test-frontend ## Run all tests (backend + frontend)

test-backend: ## Run all backend tests
	dotnet test Nexus-2.0.sln --verbosity minimal

test-frontend: ## Run all frontend tests
	cd src/frontend && npx vitest --run

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------

build: ## Build entire solution
	dotnet build Nexus-2.0.sln

clean: ## Clean build artifacts
	dotnet clean Nexus-2.0.sln
	rm -rf src/frontend/node_modules/.vite

# ---------------------------------------------------------------------------
# Docker
# ---------------------------------------------------------------------------

docker-up: ## Start full stack via Docker Compose
	docker compose -f docker/docker-compose.yml up --build

docker-local: ## Start local dev stack (PostgreSQL + Redis installed locally)
	docker compose -f docker/docker-compose.local.yml up --build

docker-down: ## Stop Docker Compose
	docker compose -f docker/docker-compose.yml down

docker-reset: ## Stop + wipe Docker volumes
	docker compose -f docker/docker-compose.yml down -v

# ---------------------------------------------------------------------------
# Database
# ---------------------------------------------------------------------------

migrate: ## Apply EF Core migrations (all services)
	@echo "Migrations auto-apply on service startup"
	@echo "To create a new migration, see: src/backend/migrations-guide.md"

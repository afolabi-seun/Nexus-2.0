-- =============================================================================
-- Nexus 2.0 — PostgreSQL Database Initialization
-- Creates a shared database with per-service schema isolation
-- =============================================================================

-- Create the shared database (if not exists)
SELECT 'CREATE DATABASE "nexusDb"' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'nexusDb')\gexec

\c "nexusDb"

CREATE SCHEMA IF NOT EXISTS nexus_security;
CREATE SCHEMA IF NOT EXISTS nexus_profile;
CREATE SCHEMA IF NOT EXISTS nexus_work;
CREATE SCHEMA IF NOT EXISTS nexus_utility;
CREATE SCHEMA IF NOT EXISTS nexus_billing;

GRANT ALL ON SCHEMA nexus_security TO postgres;
GRANT ALL ON SCHEMA nexus_profile TO postgres;
GRANT ALL ON SCHEMA nexus_work TO postgres;
GRANT ALL ON SCHEMA nexus_utility TO postgres;
GRANT ALL ON SCHEMA nexus_billing TO postgres;

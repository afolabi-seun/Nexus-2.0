# Nexus 2.0 — Frontend

React 18 single-page application for the Nexus 2.0 platform.

- **Port:** 5173 (Vite dev server)
- **Stack:** React 18, TypeScript, Vite, Tailwind CSS v3

## Tech Stack

| Category | Libraries |
|----------|-----------|
| UI | Tailwind CSS v3, lucide-react, class-variance-authority, clsx, tailwind-merge |
| Routing | React Router v6 |
| State | Zustand |
| Forms | react-hook-form, @hookform/resolvers |
| Validation | Zod v4 |
| Drag & Drop | @dnd-kit/core, @dnd-kit/sortable |
| Charts | Recharts |
| HTTP | Axios |
| Dates | date-fns |
| Testing | Vitest, @testing-library/react, fast-check |

## Features

- **Auth** — Login, platform admin login, password reset, forced password change, session restore with token refresh
- **Dashboard** — 4 summary widgets (projects, stories, tasks, sprints)
- **Projects** — CRUD, project keys, status management
- **Stories** — Create/edit with labels, links, workflow state transitions, assignment
- **Tasks** — Assignment, self-assign, time logging, department auto-routing
- **Sprints** — Planning with drag-and-drop story assignment, burndown charts, start/complete lifecycle
- **Boards** — Kanban, Sprint, Department, Backlog views with dnd-kit drag-and-drop
- **Comments** — @mentions support on stories and tasks
- **Members & Departments** — Team management, multi-department membership, role assignment
- **Settings** — Organization settings (sprint duration, story point scale)
- **Preferences** — Live theme switching (light/dark/system), date/time format
- **Invitations** — OTP-based invite flow with token validation and acceptance
- **Sessions & Devices** — View active sessions, revoke sessions, manage devices
- **Search** — Full-text search across projects, stories, and tasks
- **Reports** — 5 chart types: velocity, department workload, capacity, cycle time, task completion
- **Saved Filters** — Save and reuse filter configurations
- **Platform Admin** — Organization management, admin provisioning
- **Notifications** — Notification history and settings
- **Activity Logs** — Per-entity activity timeline
- **Billing** — Subscription management, plan comparison, usage meters, upgrade/downgrade flows

### DB-Driven Sidebar Navigation

Sidebar navigation items are fetched from ProfileService and filtered by the user's role permission level. This allows navigation structure to be managed without frontend redeployment.

## How to Run

```bash
cd src/frontend
npm install
npm run dev       # Start dev server at http://localhost:5173
npm run build     # Production build (tsc + vite build)
npm run lint      # ESLint
npm run preview   # Preview production build
```

## Environment Variables

Create a `.env` file from [`.env.example`](.env.example):

| Variable | Description |
|----------|-------------|
| `VITE_SECURITY_API_URL` | SecurityService URL (default: `http://localhost:5001`) |
| `VITE_PROFILE_API_URL` | ProfileService URL (default: `http://localhost:5002`) |
| `VITE_WORK_API_URL` | WorkService URL (default: `http://localhost:5003`) |
| `VITE_UTILITY_API_URL` | UtilityService URL (default: `http://localhost:5200`) |
| `VITE_BILLING_API_URL` | BillingService URL (default: `http://localhost:5300`) |
| `VITE_APP_NAME` | Application display name |

## Tests

93 tests across 14 test files using Vitest and fast-check for property-based testing.

```bash
npx vitest --run
```

Test files cover:
- API client and response handling
- Auth store and JWT decoding
- Form validation schemas (login, OTP, password)
- Route guards
- Theme store
- Workflow state machine
- Search validation
- Badge color mapping
- Error mapping

## Project Structure

```
src/frontend/
├── src/
│   ├── __tests__/       # 14 test files (Vitest + fast-check)
│   ├── api/             # Axios clients per service
│   ├── assets/          # Static assets
│   ├── components/      # Shared UI components
│   ├── features/        # Feature modules
│   │   ├── activity/    # Activity logs
│   │   ├── admin/       # Platform admin
│   │   ├── auth/        # Login, password reset, session restore
│   │   ├── billing/     # Subscriptions, plans, usage
│   │   ├── boards/      # Kanban, Sprint, Department, Backlog
│   │   ├── comments/    # @mentions comments
│   │   ├── dashboard/   # Dashboard widgets
│   │   ├── departments/ # Department management
│   │   ├── filters/     # Saved filters
│   │   ├── invites/     # Invitation flow
│   │   ├── members/     # Team member management
│   │   ├── notifications/ # Notification history
│   │   ├── preferences/ # Theme, date/time format
│   │   ├── projects/    # Project CRUD
│   │   ├── reports/     # Charts and reports
│   │   ├── search/      # Full-text search
│   │   ├── sessions/    # Session & device management
│   │   ├── settings/    # Organization settings
│   │   ├── sprints/     # Sprint management
│   │   ├── stories/     # Story management
│   │   └── tasks/       # Task management
│   ├── hooks/           # Custom React hooks
│   ├── pages/           # Route page components
│   ├── stores/          # Zustand stores
│   ├── test-utils/      # Test helpers
│   ├── types/           # TypeScript type definitions
│   ├── utils/           # Utility functions
│   ├── App.tsx          # Root component
│   ├── router.tsx       # Route definitions
│   └── main.tsx         # Entry point
├── .env.example
├── package.json
├── tsconfig.json
├── vite.config.ts
└── tailwind.config.js
```

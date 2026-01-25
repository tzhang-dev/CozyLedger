# Development Plan (Coding Agent)

> Execution plan for MVP implementation. This is actionable and test-driven for backend work.

## 1) Project Setup & Tooling
- Todo
  - Initialize repo structure (backend, frontend, shared docs).
  - Configure linting/formatting for .NET + frontend.
  - Add CI pipeline (build, test).
- Deliverables
  - Repo layout with `backend/`, `frontend/`, `doc/`.
  - CI config that runs tests on every push.
- Acceptance Criteria
  - `backend` builds and tests pass in CI.
  - `frontend` builds in CI.

## 2) Backend Foundations (Test-Driven)
- Todo
  - Scaffold .NET Web API with clean architecture/vertical slices.
  - Configure EF Core + PostgreSQL connection.
  - Add test project with xUnit + FluentAssertions + Testcontainers.
  - Define base entities + DbContext.
- Deliverables
  - Running API skeleton with health endpoint.
  - Test suite skeleton with at least one integration test (DB connection).
- Acceptance Criteria
  - `GET /health` returns 200.
  - Testcontainers integration test passes locally and in CI.

## 3) Auth & Membership (MVP)
- Todo
  - Implement user registration/login.
  - Implement book creation and membership join.
  - Implement invite link generation and acceptance flow.
  - Add tests for auth and invite flows.
- Deliverables
  - Auth endpoints + invite link endpoints.
  - Tests covering login, token issuance, and invite link acceptance.
- Acceptance Criteria
  - User can register/login and receive JWT.
  - Admin can generate a shareable invite link.
  - Invite link allows a new user to join a book.
  - Tests cover success + invalid link cases.

## 4) Core Domain: Accounts, Categories, Transactions
- Todo
  - Implement CRUD for accounts, categories, transactions.
  - Enforce business rules (transaction types, transfers, refunds).
  - Implement balance adjustment as a special transaction.
  - Add tests for core rules and validation.
- Deliverables
  - API endpoints for accounts, categories, transactions.
  - Validations for transfer/income/expense/refund.
  - Test suite covering rules and edge cases.
- Acceptance Criteria
  - Create/update/list for accounts and categories works.
  - Transaction list returns correct ordering.
  - Refunds stored as negative expense with `is_refund=true`.
  - Balance adjustments behave as special transactions.
  - Tests cover both valid and invalid requests.

## 5) Attachments
- Todo
  - Implement attachment upload/list/delete.
  - Add server-side checks for file type and size.
  - Ensure images >1MB are compressed below 1MB on client side (frontend).
  - Add tests for attachment metadata and storage.
- Deliverables
  - Attachment API endpoints and storage policy.
  - Client upload flow with compression.
  - Tests for attachment metadata persistence.
- Acceptance Criteria
  - Upload succeeds for images and PDFs.
  - Any image >1MB is compressed to <1MB before upload.
  - Attachment list returns correct metadata.

## 6) Reporting (MVP)
- Todo
  - Implement summary + category distribution reports.
  - Implement currency conversion rule: use most recent daily rate on or before transaction date.
  - Add tests for currency conversion edge cases.
- Deliverables
  - Report endpoints for monthly/yearly summary and category distribution.
  - Currency conversion logic with test coverage.
- Acceptance Criteria
  - Reports use book base currency.
  - Conversion uses most recent daily rate on or before transaction date.
  - Tests cover missing rate and fallback behavior.

## 7) Frontend Foundations
- Todo
  - Scaffold React + Vite + TypeScript app.
  - Configure routing, i18n, and UI framework.
  - Set up API client + TanStack Query.
- Deliverables
  - App shell with navigation (desktop + mobile).
  - i18n bundles for EN/ZH.
- Acceptance Criteria
  - App loads with dashboard/ledger/reports/accounts/members routes.
  - Language switch works.

## 8) Frontend Core Screens
- Todo
  - Implement ledger list + filters.
  - Implement transaction editor (income/expense/transfer/refund).
  - Implement accounts + categories management.
  - Implement members + invite flow UI.
- Deliverables
  - Fully functional UI for CRUD flows.
  - Form validation + error handling.
- Acceptance Criteria
  - User can create/edit transactions and see them in ledger.
  - User can manage accounts/categories.
  - User can generate invite link and share it.

## 9) Frontend Reports
- Todo
  - Implement summary + category distribution charts.
  - Ensure base currency labeling.
- Deliverables
  - Reports page with charts and summary tables.
- Acceptance Criteria
  - Reports render totals consistent with backend response.

## 10) QA & Stabilization
- Todo
  - End-to-end verification of core flows.
  - Fix any API/UI mismatches.
  - Verify localization coverage.
- Deliverables
  - Release candidate build.
  - Test report summary.
- Acceptance Criteria
  - All critical user stories pass manually.
  - Backend tests green; no failing CI.

## 11) Post-MVP (Explicitly Out of Scope)
- Recurring transactions
- Trend reports
- Import/export
- Roles/permissions
- Advanced budgets/projects


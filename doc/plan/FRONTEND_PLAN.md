# Frontend Plan (Draft)

> Planning only. No implementation yet.

## 1) Goals
- Web-only app with responsive desktop + mobile experience.
- English + Simplified Chinese (i18n) from day one.
- Match reference flows: dashboard, ledger, reports, accounts, members.

## 2) Proposed Stack (Popular/Maintained)
- Framework: React + TypeScript
- Build: Vite
- Routing: React Router
- State: TanStack Query for server state, Zustand for local UI state
- UI: Tailwind CSS + Headless UI (or Radix UI)
- Charts: ECharts or Recharts (ECharts better for complex charts)
- i18n: i18next + react-i18next
- Forms/Validation: React Hook Form + Zod
- HTTP: fetch or Axios (prefer fetch + wrappers)

## 3) Core Screens (MVP)
- Dashboard
  - Monthly summary, net income/expense, chart
- Ledger
  - Transaction list + filters (date, account, category, type, refund)
- Reports
  - Summary, category distribution (no trends in MVP)
- Accounts
  - List, create/edit, archive/hide
- Members
  - List and invite
- Auth
  - Login, register, invite acceptance
- Search
  - Global search (text + amount + date)
- Transaction Editor
  - Create/edit income, expense, transfer, refund flag

## 4) Navigation & UX
- Mobile: bottom navigation (Dashboard, Ledger, Add, Reports, Settings/Members)
- Desktop: side navigation + top bar filters
- Quick add button for transactions

## 5) i18n Plan
- Locale switch in settings
- Default locale from user profile
- Keys structure by feature (dashboard, ledger, reports, accounts)
- Category/account names from server via name_i18n

## 6) Data & API Integration Plan
- REST API integration with typed DTOs
- Caching + invalidation with TanStack Query
- Pagination for transaction list
- Search endpoints for fast filters

## 7) Attachments Plan (UI)
- Upload UI with file size display
- Client-side image compression before upload
- Supported types: images, PDF

## 8) Reporting UX
- Base currency label in all charts
- Toggle for showing refunds separately
- Drill-down from category to subcategory

## 9) Accessibility & Responsiveness
- Keyboard-friendly navigation
- Mobile-first layout
- Color contrast for charts

## 10) Open Choices
- Charting lib: ECharts vs Recharts
- UI lib: Headless UI vs Radix
- CSS: Tailwind vs CSS Modules

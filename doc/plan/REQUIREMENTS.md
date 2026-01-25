# Requirements Document (Draft)

> Planning only. No implementation details.

## 1) Goals
- Build a household bookkeeping web app with responsive desktop and mobile UI.
- Support English and Simplified Chinese from day one.
- Provide a clean export path (post-MVP).

## 2) In Scope (MVP)
- Flexible single-entry ledger (income/expense/transfer).
- One category per transaction (no split transactions).
- Multi-account support with account types.
- Categories with parent/subcategory hierarchy.
- Reports: monthly/yearly summary + category distribution (no trends in MVP).
- Multi-currency (at least CNY and EUR) with reports converted to book base currency.
- Users can belong to multiple books; each book can have multiple users (all-access, no roles/permissions yet).
- Import is not in MVP; no one-off import script deliverable for MVP.
- Refunds stored as negative expense with `is_refund=true` for reporting.
- Balance adjustments are allowed as a special transaction type.
- Attachments: receipts (image/pdf) on transactions.
- Notes + tags on transactions.
- Account archiving/hide.
- Search by text + amount + date.
- Simple account management only (create/edit/delete, no advanced rules).

## 3) Out of Scope (MVP)
- Recurring transactions.
- Roles/permissions.
- Complex projects/budgets beyond a basic plan.
- Mobile native apps (web only).
- Export JSON/CSV for backup (post-MVP).

## 4) User Stories (MVP)
- As a user, I can create a book and invite members.
- As a user, I can add accounts with types and currency.
- As a user, I can add income, expense, and transfer transactions.
- As a user, I can add categories and subcategories in EN/ZH.
- As a user, I can view my ledger with filters by date, account, category.
- As a user, I can view monthly/yearly summaries and category charts.
- As an admin, I can invite members by sharing a link (email optional, not required in MVP).
- As a user, I can attach receipts (image/pdf) to transactions.
- As a user, I can add notes and tags to transactions.
- As a user, I can hide/archive accounts.
- As a user, I can search transactions by text, amount, and date.

## 5) Acceptance Criteria (High Level)
- Ledger lists all transactions in correct date order.
- Balance is derived from transactions; no manual balance edits.
- Refunds reduce expense totals and can be shown separately in reports.
- UI labels and category/account names switch between EN and ZH.
- Transaction timestamps are stored in UTC and displayed in local timezone.
- Attachments: any image larger than 1MB must be compressed to under 1MB before upload.

## 6) Non-Functional Requirements
- Data integrity via transaction-only balance updates.
- Basic auditability (created_by + timestamps).
- Performance targets for listing and reporting in typical datasets.
- Privacy: only members of a book can access its data.
## 7) Currency Handling (Planning)
- Each book has a base currency.
- Transactions keep their original currency.
- Reports convert amounts into base currency for totals.
- Exchange rates come from an external feed.
- Use the most recent available daily rate on or before the transaction date for reporting conversion.
- Exchange rates are refreshed daily.

## 8) Backup (Planning)
- Daily backups.

## 9) Open Questions
- Is CSV export required for all entities or only transactions?

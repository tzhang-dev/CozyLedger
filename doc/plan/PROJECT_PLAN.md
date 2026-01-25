# Bookkeeper Project Plan (Draft)

> Planning only. No implementation details.

## 1) MVP Scope (Locked)
- Flexible single-entry ledger (income/expense/transfer).
- One category per transaction (no splits).
- No recurring transactions in MVP.
- Multi-account support with account types.
- Categories with parent/subcategory hierarchy.
- Reports: monthly/yearly summary + category distribution (no trends in MVP).
- Collaboration: users can belong to multiple books; each book can have multiple users (all-access, no roles yet).
- Import/Export: not in MVP; no one-off import script deliverable for MVP.
- i18n: English + Simplified Chinese from day one.
- Refunds: stored as negative expenses + `is_refund=true` for reporting.
- Attachments: receipts (image/pdf) on transactions.
- Notes + tags on transactions.
- Account archiving/hide.
- Search by text + amount + date.

## 2) Architecture Plan (High-Level)
- Backend: .NET REST API
- Database: PostgreSQL
- API documentation: OpenAPI (Swagger)
- Authentication: basic user login + shared book membership
- Data integrity: balances derived from transactions (no direct edits)
- Currency handling: reports convert to book base currency
- Exchange rates: sourced from an external feed
- Exchange rate job: daily
- Backup job: daily

## 3) Data Model Plan (Conceptual)
- User: id, email, display_name, locale
- Book: id, name, base_currency, created_at
- Membership: book_id, user_id
- Account: id, book_id, name_i18n, type, currency, is_hidden, include_in_net_worth, note
- Category: id, book_id, parent_id, name_i18n, type (income|expense), is_active
- Transaction:
  - type: expense|income|transfer|balance_adjustment|liability_adjustment
  - date, amount, currency
  - account_id, to_account_id (transfer only)
  - category_id (expense/income only)
  - member_id, note, is_refund
  - created_by_user_id
- Optional: Attachment, AuditLog
- Optional (planning): ExchangeRate for base-currency reporting
- Optional (planning): Tag, TransactionTag

## 4) OpenAPI Plan (Structure Only)
- Auth: register, login
- Books & Members: list/create books, invite via shareable link (email optional), list members
- Accounts: list/create/update
- Categories: list/create/update
- Transactions: list/create/update; filters by date/account/category/type/refund
- Attachments: upload/list/delete per transaction
- Tags: list/create/update, assign to transactions
- Search: text + amount + date
- Reports: summary, category distribution
- Import/Export: post-MVP

## 5) i18n Plan
- Translatable fields stored as name_i18n JSON (en, zh-Hans)
- UI string bundles: en.json, zh-Hans.json
- Fallback to English if missing

## 6) UI Plan (Web Responsive)
Pages:
- Dashboard: monthly summary, summary chart, key totals
- Ledger: transaction list + filters
- Reports: summary + category chart (pie/bar)
- Accounts: list + add/edit
- Members: invite/list
- Search: global search across text/amount/date

Navigation:
- Mobile: bottom nav
- Desktop: side nav

## 7) MVP Timeline (Planning)
- Week 1: requirements lock + data model plan + API plan
- Week 2: import/export plan + reporting plan + i18n plan
- Week 3: UX flow plan + risk review

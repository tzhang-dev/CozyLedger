# Reporting Specification (Draft)

> Planning only. No implementation details.

## 1) Summary Reports
- Monthly summary: total income, total expense, net balance.
- Yearly summary: total income, total expense, net balance.
- All totals are converted into the book base currency.
- Exchange rates are sourced from an external feed.
- Use the most recent available daily rate on or before the transaction date for conversion.
- Exchange rates are refreshed daily.

## 2) Category Distribution
- Expense by category (pie/bar)
- Income by category (bar)
- Support drill-down to subcategory

## 3) Trend Reports (Post-MVP)
- Monthly trend of income vs expense
- Optional daily trend in a selected month

## 4) Additional Household Reports (Planned)
- Net worth trend (by month)
- Cashflow report (income vs expenses over time)
- Top merchants/payees is not needed (merchant removed)
- Top categories (top N spending)
- Category comparison: current period vs previous period
- Account balance trend per account
- Transfer volume (by month)
- Refunds trend (by month)
- Expense by member (if member is set)
- Income source breakdown (by category)

## 5) Refund Handling
- Refunds reduce expense totals (net expense).
- Optional refunds breakdown in reports as separate line or section.

## 6) Filters
- date range
- account
- category
- type (income/expense/transfer)
- refund flag

## 7) Output
- Provide numbers for charting and a small table summary.
- Return localized labels based on user locale.
- Include the base currency code in all report responses.

# TO_DECIDE

## 2026-02-16 - Reporting currency conversion fallback and missing-rate behavior
- Decision: Report conversion first uses a direct rate (`txnCurrency -> baseCurrency`) with the most recent `EffectiveDateUtc` on or before the transaction date.
- Decision: If direct rate is missing, fallback to inverse rate (`baseCurrency -> txnCurrency`) on or before the transaction date and convert by division.
- Decision: If neither direct nor inverse rate exists, the report endpoint returns `422 Unprocessable Entity` with a `missingRates` list instead of silently skipping transactions.
- Rationale: This keeps totals deterministic, preserves data quality, and exposes missing exchange-rate data clearly for remediation.

## 2026-02-16 - Frontend bundle size warning handling for MVP
- Decision: Keep the current single-chunk frontend build for MVP even though Vite warns about a large JS bundle (>500KB).
- Rationale: Functional completeness and flow stability were prioritized for MVP milestones 8-10; route-level code splitting can be added as a post-MVP optimization without changing API behavior.

## 2026-02-17 - Docker Compose postgres host port binding
- Decision: Do not publish postgres port `5432` to the host in `docker-compose.yml`.
- Rationale: Host `5432` is commonly occupied by local Postgres installs; backend and frontend communicate with DB over the compose network, so host exposure is unnecessary for normal bundled app startup/shutdown.

## 2026-02-22 - Figma UI alignment, i18n wiring, and base-currency source of truth
- Decision: Keep the Figma-aligned visual treatment while implementing page-scoped CSS overrides (not global redesign primitives) to avoid regressions on non-target pages.
- Decision: Move hardcoded UI copy to `i18n` keys and persist locale in `localStorage`; use `zh-CN`/`en-US` formatting for localized month labels.
- Decision: Treat the active book's base currency as the frontend fallback source for Home/Reports and scope `books` query cache keys by auth token (`['books', token]`) to avoid stale cross-session currency values.
- Rationale: This preserves visual parity goals, ensures language changes apply consistently, and prevents incorrect currency labels caused by generic defaults or stale cached book data.

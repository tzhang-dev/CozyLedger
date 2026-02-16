# TO_DECIDE

## 2026-02-16 - Reporting currency conversion fallback and missing-rate behavior
- Decision: Report conversion first uses a direct rate (`txnCurrency -> baseCurrency`) with the most recent `EffectiveDateUtc` on or before the transaction date.
- Decision: If direct rate is missing, fallback to inverse rate (`baseCurrency -> txnCurrency`) on or before the transaction date and convert by division.
- Decision: If neither direct nor inverse rate exists, the report endpoint returns `422 Unprocessable Entity` with a `missingRates` list instead of silently skipping transactions.
- Rationale: This keeps totals deterministic, preserves data quality, and exposes missing exchange-rate data clearly for remediation.

# Backend Technical Plan (Draft)

> Planning only. No implementation yet.

## 1) Architecture Overview
- Style: Modular monolith (vertical slices by domain)
- API: REST only
- Data: PostgreSQL
- Docs: OpenAPI/Swagger

## 2) Proposed Modules
- Identity & Auth
- Books & Membership
- Accounts
- Categories
- Transactions
- Tags
- Attachments
- Reports
- Exchange Rates
- Search
- Background Jobs
- Observability

## 3) Technology Stack (Proposed, Actively Maintained)
### Core
- .NET 10 (LTS)
- ASP.NET Core Web API

### Data Access
- EF Core 10 (ORM)
- Npgsql (PostgreSQL provider)
- FluentValidation (input validation)

### Auth & Security
- ASP.NET Core Identity (optional; for user + password mgmt)
- JWT Bearer auth (Microsoft.AspNetCore.Authentication.JwtBearer)
- BCrypt.Net-Next (password hashing if not using Identity)

### API Docs
- Swashbuckle.AspNetCore (OpenAPI/Swagger UI)

### Mapping / DTO
- Mapperly (compile-time mapping) or Mapster (runtime)

### JSON & Serialization
- System.Text.Json (built-in)

### Background Jobs
- Hangfire (PostgreSQL storage) for daily exchange rate + backup jobs
  - Alternative: Quartz.NET

### Exchange Rate Feeds
- Choose provider later (e.g., Open Exchange Rates, Exchangerate.host)
- HTTP client: IHttpClientFactory + Polly for retries

### File/Attachment Handling
- Local storage in MVP (filesystem) + metadata in DB
- Image processing: SixLabors.ImageSharp (resize/compress)
- PDF handling: store as-is (no conversion)

### Search
- PostgreSQL full-text search (tsvector) for text search
- Range filters for amount/date

### Logging / Metrics
- Serilog (structured logging)
- OpenTelemetry (traces/metrics)

### Testing
- xUnit
- FluentAssertions
- Testcontainers for PostgreSQL integration tests
 - Backend development is test-driven; new development must be accompanied by concrete tests.

## 4) Module Details (Short)
- Identity/Auth: registration, login, JWT refresh
- Books/Members: book creation + member invite
- Accounts: CRUD + archive/hide
- Categories: CRUD + parent/child
- Transactions: CRUD, validation rules, refund flag
- Tags: CRUD + assignment
- Attachments: upload, compress images <1MB, list/delete
- Reports: summary, category distributions, conversion to base currency (no trends in MVP)
- Exchange Rates: daily fetch + store
- Search: text/amount/date filters
- Background Jobs: daily exchange rate + daily backup

## 5) Data & Cross-Cutting Policies
- UTC timestamps stored; display local time at UI
- Daily exchange rate refresh
- Daily backup
- Audit on create/edit/delete
- Data model constraints are enforced at API layer (DB kept loose/minimal).

## 6) Risks / Open Choices
- Choose exchange-rate provider (cost, limits, SLA)
- Decide on Identity vs custom user table
- Decide on Hangfire vs Quartz
- Decide on attachment storage path policy

## 7) Decisions Needed (with Options and Tradeoffs)
### A) ORM / Data Access
- Option 1: EF Core
  - Pros: mature, strong tooling, migrations, LINQ, good community support
  - Cons: can be heavier; complex queries need tuning
- Option 2: Dapper
  - Pros: fast, simple, full SQL control
  - Cons: more manual mapping, fewer guardrails
Decision: EF Core.

### B) Auth Model
- Option 1: ASP.NET Core Identity
  - Pros: built-in password management, lockout, reset flows
  - Cons: more schema complexity
- Option 2: Custom user table + JWT
  - Pros: simple, lean
  - Cons: must implement password flows manually
Decision: ASP.NET Core Identity.

### C) Background Jobs
- Option 1: Hangfire (PostgreSQL storage)
  - Pros: dashboard, retries, easy scheduling
  - Cons: adds UI/admin surface
- Option 2: Quartz.NET
  - Pros: lightweight scheduler, no dashboard
  - Cons: less visibility/ops tooling
Decision: Quartz.NET.

### D) Exchange Rate Provider
- Option 1: Paid provider (higher reliability)
- Option 2: Free provider (lower reliability/limits)
Decision: Free provider (start simple; can upgrade later).

### E) Mapping Library
- Option 1: Mapperly (compile-time)
  - Pros: fast, no runtime reflection
  - Cons: less dynamic
- Option 2: Mapster (runtime)
  - Pros: flexible, quick to use
  - Cons: runtime overhead
Decision: Mapperly.

### F) Attachment Storage
- Option 1: Local filesystem (MVP)
  - Pros: simple, fast
  - Cons: scaling and backup complexity
- Option 2: Object storage (S3-compatible)
  - Pros: scalable, easier backup
  - Cons: extra infra
Decision: Local filesystem (MVP).

### G) Search
- Option 1: PostgreSQL full-text search
  - Pros: no extra service
  - Cons: limited advanced features
- Option 2: Dedicated search (Elastic/Meili)
  - Pros: rich search features
  - Cons: more infrastructure
Decision: PostgreSQL full-text search.

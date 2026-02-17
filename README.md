# CozyLedger

CozyLedger is a household bookkeeping web app for personal/family collaboration.

## Features (MVP)

- Register/login with JWT auth.
- Create one or more books (shared ledgers).
- Manage accounts and categories.
- Record income, expense, transfer, and balance adjustment transactions.
- Mark expense refunds (stored as negative expenses).
- Upload transaction attachments (image/pdf).
- Monthly reports and category distribution in book base currency.
- EN/ZH UI language switch.

## Tech stack

- Frontend: React + Vite + TypeScript
- Backend: ASP.NET Core (.NET 10)
- Database: PostgreSQL 16

## Prerequisites

For Docker usage:
- Docker Desktop (or Docker Engine + Compose)

For local non-Docker usage:
- .NET SDK 10
- Node.js 22+
- PostgreSQL 16+

## Quick start (Docker, recommended)

Start the full stack:

```bash
docker compose up --build -d
```

Open:
- App UI: `http://localhost:5173`
- API health: `http://localhost:8080/health`

Stop services:

```bash
docker compose down
```

Stop and remove data volumes (resets DB + stored attachments):

```bash
docker compose down -v
```

## Local development (without Docker)

1. Start PostgreSQL and create a database/user matching backend config:
   - DB: `cozyledger_dev`
   - User: `cozy`
   - Password: `cozy`
   - Port: `5432`

2. Start backend API from repo root:

```bash
dotnet run --project backend/src/CozyLedger.Api/CozyLedger.Api.csproj
```

Default backend URL (dev profile): `http://localhost:5245`

3. Start frontend (new terminal):

```bash
cd frontend
npm ci
$env:VITE_API_BASE_URL="http://localhost:5245"
npm run dev -- --host 0.0.0.0 --port 5173
```

Open `http://localhost:5173`.

## First-time app usage

1. Open the app at `http://localhost:5173`.
2. In the setup screen:
   - Register (or login) with email/password.
   - Create a book with a base currency (for example `USD`).
3. Use bottom navigation:
   - `Transactions`: list ledger entries.
   - `New`: create a transaction.
   - `Reports`: summary + category charts.
   - `Accounts`: manage accounts.
   - `Settings`: switch language, manage categories, sign out.

## Common workflows

### Accounts

- Go to `Accounts`.
- Use the top-right action menu to add/edit/delete accounts.
- Click an account row to open details modal and view computed balance.

### Categories

- Go to `Settings` -> `Manage categories`.
- Add/edit categories (income or expense).

### Transactions

- Go to `New` to create a transaction.
- Select type:
  - Expense/Income: requires category.
  - Transfer: requires destination account.
  - Balance Adjustment: no category/destination account.
- Refund is available only for expense entries.

### Invites / shared books

- Members screen is available at route `http://localhost:5173/members`.
- Generate invite token/link from one account.
- Accept invite token from another signed-in user.

### Reports

- Go to `Reports`.
- Select year/month and category view (income/expense).
- Values are shown in book base currency.

## Attachment limits

- Allowed types: `image/jpeg`, `image/png`, `application/pdf`
- Image size: <= 1 MB after compression
- PDF size: <= 5 MB

## Useful API endpoint

- Health check: `GET /health`

Example:

```bash
curl http://localhost:8080/health
```

## Troubleshooting

- Frontend cannot reach backend:
  - Ensure `VITE_API_BASE_URL` matches backend URL when running locally.
- Database connection errors:
  - Verify PostgreSQL is running and backend connection string values are correct.
- Docker startup issues:
  - Run `docker compose logs -f backend` and `docker compose logs -f frontend`.
- Reset local Docker data:
  - `docker compose down -v`

## Run tests

Backend tests:

```bash
dotnet test backend/tests/CozyLedger.Api.Tests/CozyLedger.Api.Tests.csproj
```

Frontend checks:

```bash
cd frontend
npm run lint
npm run build
```

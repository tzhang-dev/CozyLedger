# Feature User Story Template (Simple)

## 1) Basic Info

- **Feature ID**: `FEAT-DOC-001`
- **Title**: `Rework plan docs into project documentation`
- **Status**: `new`

## 2) User Story

As a **project maintainer**, I want **`doc/plan` reworked from MVP execution notes into stable project documentation** so that **the repository contains maintainable, accurate reference docs instead of obsolete planning artifacts**.

## 3) Requirements

- Identify and classify each file in `doc/plan/` as: keep-and-rewrite, merge-into-other-doc, archive, or remove.
- Replace MVP task sequencing content with durable documentation sections (architecture, domain model, key workflows, API/reporting behavior, and operational conventions).
- Preserve historical context only where useful by moving obsolete execution details into an explicit archive section or archive file.
- Ensure documentation language reflects the current state: MVP completed, with future work tracked as backlog/roadmap instead of implementation milestones.
- Update cross-references so links between `README.md`, `doc/plan/`, and any feature docs remain valid.

## 4) Acceptance Criteria

1. Given the existing `doc/plan/` folder, when the rework is complete, then every file has a defined role (current reference doc, merged content, archived content, or removed obsolete file).
2. Given a new contributor, when they read the updated docs, then they can understand current system architecture, core domain concepts, reporting rules, and where to find implementation details without relying on MVP milestone plans.
3. Given previously MVP-focused docs (such as `DEV_PLAN.md`), when reviewed after rework, then they no longer read as pending implementation tasks and instead describe current-state documentation plus forward-looking roadmap/backlog.

## 5) Implementation Notes

- Use `doc/plan/DEV_PLAN.md` as the migration anchor, converting it into a documentation index and ownership map rather than an execution checklist.
- Consolidate overlapping sections from `PROJECT_PLAN.md`, `FRONTEND_PLAN.md`, `BACKEND_TECH_PLAN.md`, and `REPORTING_SPEC.md` into fewer authoritative docs.
- Keep `TO_DECIDE.md` for unresolved product/technical decisions, but remove items that were already settled during MVP delivery.
- Prefer concise, reference-style writing over speculative or temporary planning language.

## 6) Tests

- **Tests to run**:
  - `n/a (documentation-only story creation)`
- **Coverage to add/update**:
  - `n/a`
- **Result summary**:
  - `n/a`

## 7) Done Checklist

- [ ] Requirements implemented.
- [ ] Acceptance criteria met.
- [ ] Tests added/updated and run.
- [ ] Docs updated if needed.
